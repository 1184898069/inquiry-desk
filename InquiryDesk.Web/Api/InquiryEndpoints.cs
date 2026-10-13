using System.Text;
using InquiryDesk.Web.Data;
using InquiryDesk.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace InquiryDesk.Web.Api;

/// <summary>
/// 询盘相关的全部 HTTP 接口。这里只做三件事：解析参数、调用领域规则、组装返回。
/// 业务规则（状态流转、到期判断、统计）都在 Domain 层，不要往这里塞。
/// </summary>
public static class InquiryEndpoints
{
    /// <summary>把所有接口挂到应用上，Program.cs 里调用一次即可。</summary>
    public static void MapInquiryEndpoints(this WebApplication app)
    {
        app.MapGet("/api/inquiries", GetListAsync);
        app.MapPost("/api/inquiries", CreateAsync);
        app.MapGet("/api/inquiries/due", GetDueAsync);
        app.MapGet("/api/inquiries/export", ExportAsync);
        app.MapGet("/api/inquiries/{id:int}", GetDetailAsync);
        app.MapPut("/api/inquiries/{id:int}/status", UpdateStatusAsync);
        app.MapPost("/api/inquiries/{id:int}/notes", AddNoteAsync);
        app.MapGet("/api/stats", GetStatsAsync);
    }

    /// <summary>列表查询，可按状态、意向筛选（参数传枚举名，如 status=FollowingUp）。</summary>
    private static async Task<IResult> GetListAsync(AppDbContext db, string? status, string? intent)
    {
        InquiryStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseEnum(status, out InquiryStatus parsedStatus))
            {
                return BadRequest($"状态参数不合法：{status}");
            }
            statusFilter = parsedStatus;
        }
        IntentLevel? intentFilter = null;
        if (!string.IsNullOrWhiteSpace(intent))
        {
            if (!TryParseEnum(intent, out IntentLevel parsedIntent))
            {
                return BadRequest($"意向参数不合法：{intent}");
            }
            intentFilter = parsedIntent;
        }

        List<Inquiry> inquiries = await db.Inquiries
            .OrderByDescending(inquiry => inquiry.CreatedAt)
            .ToListAsync();

        // Demo 数据量很小，筛选放内存里写直白循环；以后数据量大了再把条件翻译成 SQL，行为不变
        DateTime now = DateTime.Now;
        List<InquiryListItemDto> result = new List<InquiryListItemDto>();
        foreach (Inquiry inquiry in inquiries)
        {
            if (statusFilter != null && inquiry.Status != statusFilter.Value)
            {
                continue;
            }
            if (intentFilter != null && inquiry.Intent != intentFilter.Value)
            {
                continue;
            }
            result.Add(InquiryMapping.ToListItem(inquiry, now));
        }
        return Results.Ok(result);
    }

    /// <summary>新建一条询盘，初始状态固定为新询盘。</summary>
    private static async Task<IResult> CreateAsync(AppDbContext db, CreateInquiryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return BadRequest("客户名不能为空。");
        }
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("询盘内容不能为空。");
        }
        if (!TryParseEnum(request.Source, out InquirySource source))
        {
            return BadRequest("来源不合法，应为 Website / Exhibition / Referral / Phone / Other。");
        }
        if (!TryParseEnum(request.Intent, out IntentLevel intent))
        {
            return BadRequest("意向等级不合法，应为 High / Medium / Low。");
        }

        Inquiry inquiry = new Inquiry
        {
            CustomerName = request.CustomerName.Trim(),
            ContactInfo = (request.ContactInfo ?? "").Trim(),
            Source = source,
            Content = request.Content.Trim(),
            Intent = intent,
            Status = InquiryStatus.New,
            NextFollowUpAt = request.NextFollowUpAt,
            CreatedAt = DateTime.Now
        };
        db.Inquiries.Add(inquiry);
        await db.SaveChangesAsync();

        InquiryDetailDto dto = InquiryMapping.ToDetail(inquiry, DateTime.Now);
        return Results.Created($"/api/inquiries/{inquiry.Id}", dto);
    }

    /// <summary>查一条询盘的详情，含全部跟进记录。</summary>
    private static async Task<IResult> GetDetailAsync(AppDbContext db, int id)
    {
        Inquiry? inquiry = await FindWithNotesAsync(db, id);
        if (inquiry == null)
        {
            return NotFound(id);
        }
        return Results.Ok(InquiryMapping.ToDetail(inquiry, DateTime.Now));
    }

    /// <summary>变更状态。流转是否合法由实体上的领域规则说了算，这里只负责把拒绝原因转成 400。</summary>
    private static async Task<IResult> UpdateStatusAsync(AppDbContext db, int id, UpdateStatusRequest request)
    {
        if (!TryParseEnum(request.Status, out InquiryStatus newStatus))
        {
            return BadRequest("目标状态不合法，应为 New / Contacted / FollowingUp / Won / Lost。");
        }
        Inquiry? inquiry = await FindWithNotesAsync(db, id);
        if (inquiry == null)
        {
            return NotFound(id);
        }

        try
        {
            inquiry.ChangeStatus(newStatus);
        }
        catch (InvalidOperationException ex)
        {
            // 领域规则抛出的消息就是给用户看的原因，原样返回
            return BadRequest(ex.Message);
        }
        await db.SaveChangesAsync();
        return Results.Ok(InquiryMapping.ToDetail(inquiry, DateTime.Now));
    }

    /// <summary>追加一条跟进记录；请求里带了下次跟进时间就一并更新，不带则保持不变。</summary>
    private static async Task<IResult> AddNoteAsync(AppDbContext db, int id, AddNoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("跟进记录内容不能为空。");
        }
        Inquiry? inquiry = await FindWithNotesAsync(db, id);
        if (inquiry == null)
        {
            return NotFound(id);
        }

        FollowUpNote note = new FollowUpNote
        {
            Content = request.Content.Trim(),
            CreatedAt = DateTime.Now
        };
        inquiry.Notes.Add(note);
        if (request.NextFollowUpAt != null)
        {
            inquiry.NextFollowUpAt = request.NextFollowUpAt;
        }
        await db.SaveChangesAsync();
        return Results.Ok(InquiryMapping.ToDetail(inquiry, DateTime.Now));
    }

    /// <summary>到期提醒列表：已逾期的排前面，同类里跟进时间早的排前面。</summary>
    private static async Task<IResult> GetDueAsync(AppDbContext db)
    {
        List<Inquiry> inquiries = await db.Inquiries.ToListAsync();
        DateTime now = DateTime.Now;

        List<Inquiry> dueInquiries = new List<Inquiry>();
        foreach (Inquiry inquiry in inquiries)
        {
            if (inquiry.GetDueKind(now) != DueKind.None)
            {
                dueInquiries.Add(inquiry);
            }
        }
        dueInquiries.Sort((left, right) => CompareByUrgency(left, right, now));

        List<InquiryListItemDto> result = new List<InquiryListItemDto>();
        foreach (Inquiry inquiry in dueInquiries)
        {
            result.Add(InquiryMapping.ToListItem(inquiry, now));
        }
        return Results.Ok(result);
    }

    /// <summary>顶部统计卡片的数据：总数、各状态/意向计数、逾期数、今日待跟数。</summary>
    private static async Task<IResult> GetStatsAsync(AppDbContext db)
    {
        List<Inquiry> inquiries = await db.Inquiries.ToListAsync();
        StatsResult stats = StatsCalculator.Compute(inquiries, DateTime.Now);
        return Results.Ok(InquiryMapping.ToStatsDto(stats));
    }

    /// <summary>导出全部询盘为 CSV（中文列名和中文枚举值，Excel 可直接打开）。</summary>
    private static async Task<IResult> ExportAsync(AppDbContext db)
    {
        List<Inquiry> inquiries = await db.Inquiries
            .OrderByDescending(inquiry => inquiry.CreatedAt)
            .ToListAsync();

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("编号,客户名,联系方式,来源,询盘内容,意向等级,状态,下次跟进时间,创建时间");
        foreach (Inquiry inquiry in inquiries)
        {
            csv.AppendLine(BuildCsvRow(inquiry));
        }

        // Excel 打开 UTF-8 的 CSV 需要 BOM 才不乱码，这里手动在最前面拼 BOM 字节
        byte[] bodyBytes = Encoding.UTF8.GetBytes(csv.ToString());
        byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
        byte[] bytes = new byte[bom.Length + bodyBytes.Length];
        bom.CopyTo(bytes, 0);
        bodyBytes.CopyTo(bytes, bom.Length);

        return Results.File(bytes, "text/csv; charset=utf-8", "inquiries.csv");
    }

    private static string BuildCsvRow(Inquiry inquiry)
    {
        string[] fields = new string[]
        {
            inquiry.Id.ToString(),
            inquiry.CustomerName,
            inquiry.ContactInfo,
            EnumText.Source(inquiry.Source),
            inquiry.Content,
            EnumText.Intent(inquiry.Intent),
            EnumText.Status(inquiry.Status),
            inquiry.NextFollowUpAt?.ToString("yyyy-MM-dd HH:mm") ?? "",
            inquiry.CreatedAt.ToString("yyyy-MM-dd HH:mm")
        };

        List<string> escapedFields = new List<string>();
        foreach (string field in fields)
        {
            escapedFields.Add(EscapeCsvField(field));
        }
        return string.Join(",", escapedFields);
    }

    /// <summary>CSV 转义：字段里有逗号、引号或换行时用双引号包起来，内部引号翻倍。</summary>
    private static string EscapeCsvField(string field)
    {
        bool needsQuotes = field.Contains(',') || field.Contains('"') || field.Contains('\n');
        if (!needsQuotes)
        {
            return field;
        }
        string doubledQuotes = field.Replace("\"", "\"\"");
        return "\"" + doubledQuotes + "\"";
    }

    /// <summary>按编号查询盘并带上跟进记录，查不到返回 null。多个接口共用这一句查询。</summary>
    private static async Task<Inquiry?> FindWithNotesAsync(AppDbContext db, int id)
    {
        Inquiry? inquiry = await db.Inquiries
            .Include(item => item.Notes)
            .FirstOrDefaultAsync(item => item.Id == id);
        return inquiry;
    }

    /// <summary>到期排序比较：先比紧急程度（逾期 &gt; 今日待跟），再比跟进时间先后。</summary>
    private static int CompareByUrgency(Inquiry left, Inquiry right, DateTime now)
    {
        int rankCompare = UrgencyRank(right.GetDueKind(now)).CompareTo(UrgencyRank(left.GetDueKind(now)));
        if (rankCompare != 0)
        {
            return rankCompare;
        }
        return Nullable.Compare(left.NextFollowUpAt, right.NextFollowUpAt);
    }

    private static int UrgencyRank(DueKind kind)
    {
        switch (kind)
        {
            case DueKind.Overdue:
                return 2;
            case DueKind.DueToday:
                return 1;
            default:
                return 0;
        }
    }

    /// <summary>
    /// 把字符串解析成枚举。只接受枚举名（如 "FollowingUp"，不分大小写）；
    /// Enum.TryParse 连 "1" 这种数字串也能解析成功，这里显式拒绝，防止误传。
    /// </summary>
    private static bool TryParseEnum<TEnum>(string? text, out TEnum value) where TEnum : struct, Enum
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }
        string trimmed = text.Trim();
        if (!Enum.TryParse(trimmed, ignoreCase: true, out value))
        {
            return false;
        }
        bool looksLikeNumber = int.TryParse(trimmed, out _);
        return Enum.IsDefined(value) && !looksLikeNumber;
    }

    private static IResult BadRequest(string message)
    {
        return Results.BadRequest(new ErrorDto { Error = message });
    }

    private static IResult NotFound(int id)
    {
        return Results.NotFound(new ErrorDto { Error = $"没有编号为 {id} 的询盘。" });
    }
}
