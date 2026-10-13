using InquiryDesk.Web.Domain;

namespace InquiryDesk.Web.Api;

/// <summary>实体 → DTO 的映射集中放这里，端点代码只管流程。</summary>
public static class InquiryMapping
{
    /// <summary>把一条询盘映射成列表行。dueKind 需要当前时间，由调用方传入。</summary>
    public static InquiryListItemDto ToListItem(Inquiry inquiry, DateTime now)
    {
        InquiryListItemDto dto = new InquiryListItemDto();
        FillListFields(dto, inquiry, now);
        return dto;
    }

    /// <summary>把一条询盘（含跟进记录）映射成详情。记录按时间从早到晚排，像聊天记录一样读。</summary>
    public static InquiryDetailDto ToDetail(Inquiry inquiry, DateTime now)
    {
        InquiryDetailDto dto = new InquiryDetailDto();
        FillListFields(dto, inquiry, now);

        List<FollowUpNote> sortedNotes = inquiry.Notes
            .OrderBy(note => note.CreatedAt)
            .ToList();
        foreach (FollowUpNote note in sortedNotes)
        {
            dto.Notes.Add(new NoteDto
            {
                Id = note.Id,
                Content = note.Content,
                CreatedAt = note.CreatedAt
            });
        }
        return dto;
    }

    private static void FillListFields(InquiryListItemDto dto, Inquiry inquiry, DateTime now)
    {
        dto.Id = inquiry.Id;
        dto.CustomerName = inquiry.CustomerName;
        dto.ContactInfo = inquiry.ContactInfo;
        dto.Source = inquiry.Source.ToString();
        dto.Content = inquiry.Content;
        dto.Intent = inquiry.Intent.ToString();
        dto.Status = inquiry.Status.ToString();
        dto.NextFollowUpAt = inquiry.NextFollowUpAt;
        dto.CreatedAt = inquiry.CreatedAt;
        dto.DueKind = inquiry.GetDueKind(now).ToString();
    }

    /// <summary>把统计结果映射成接口形状（枚举 key 换成名字字符串）。</summary>
    public static StatsDto ToStatsDto(StatsResult stats)
    {
        StatsDto dto = new StatsDto
        {
            Total = stats.Total,
            OverdueCount = stats.OverdueCount,
            DueTodayCount = stats.DueTodayCount
        };
        foreach (KeyValuePair<InquiryStatus, int> pair in stats.ByStatus)
        {
            dto.ByStatus[pair.Key.ToString()] = pair.Value;
        }
        foreach (KeyValuePair<IntentLevel, int> pair in stats.ByIntent)
        {
            dto.ByIntent[pair.Key.ToString()] = pair.Value;
        }
        return dto;
    }
}
