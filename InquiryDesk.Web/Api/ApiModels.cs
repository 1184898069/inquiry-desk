namespace InquiryDesk.Web.Api;

// 说明：请求/响应用单独的 DTO，不直接拿实体进出。
// 枚举一律用名字字符串（如 "FollowingUp"），前端和 curl 调试时都能直接看懂。

/// <summary>新建询盘的请求体。</summary>
public class CreateInquiryRequest
{
    public string? CustomerName { get; set; }
    public string? ContactInfo { get; set; }

    /// <summary>来源枚举名：Website / Exhibition / Referral / Phone / Other。</summary>
    public string? Source { get; set; }

    public string? Content { get; set; }

    /// <summary>意向枚举名：High / Medium / Low。</summary>
    public string? Intent { get; set; }

    public DateTime? NextFollowUpAt { get; set; }
}

/// <summary>变更状态的请求体。</summary>
public class UpdateStatusRequest
{
    /// <summary>目标状态枚举名：New / Contacted / FollowingUp / Won / Lost。</summary>
    public string? Status { get; set; }
}

/// <summary>新增跟进记录的请求体。</summary>
public class AddNoteRequest
{
    public string? Content { get; set; }

    /// <summary>可选：同时把下次跟进时间改到这个时刻；不传则保持不变。</summary>
    public DateTime? NextFollowUpAt { get; set; }
}

/// <summary>列表里一行询盘的形状（不含跟进记录）。</summary>
public class InquiryListItemDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string ContactInfo { get; set; } = "";
    public string Source { get; set; } = "";
    public string Content { get; set; } = "";
    public string Intent { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? NextFollowUpAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>到期分类枚举名：None / DueToday / Overdue，由服务端按当前时间算好。</summary>
    public string DueKind { get; set; } = "None";
}

/// <summary>详情的形状：列表字段 + 全部跟进记录。</summary>
public class InquiryDetailDto : InquiryListItemDto
{
    public List<NoteDto> Notes { get; set; } = new List<NoteDto>();
}

/// <summary>一条跟进记录的形状。</summary>
public class NoteDto
{
    public int Id { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

/// <summary>/api/stats 的返回形状，分组计数用枚举名做 key。</summary>
public class StatsDto
{
    public int Total { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> ByIntent { get; set; } = new Dictionary<string, int>();
    public int OverdueCount { get; set; }
    public int DueTodayCount { get; set; }
}

/// <summary>统一的错误返回形状：{ "error": "给人看的原因" }。</summary>
public class ErrorDto
{
    public string Error { get; set; } = "";
}
