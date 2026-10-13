namespace InquiryDesk.Web.Domain;

/// <summary>统计结果：顶部卡片和 /api/stats 接口共用这个形状。</summary>
public class StatsResult
{
    /// <summary>询盘总数。</summary>
    public int Total { get; set; }

    /// <summary>按状态计数，所有状态都会出现（没有的记 0）。</summary>
    public Dictionary<InquiryStatus, int> ByStatus { get; set; } = new Dictionary<InquiryStatus, int>();

    /// <summary>按意向等级计数，所有等级都会出现（没有的记 0）。</summary>
    public Dictionary<IntentLevel, int> ByIntent { get; set; } = new Dictionary<IntentLevel, int>();

    /// <summary>已逾期的询盘数（终态不计）。</summary>
    public int OverdueCount { get; set; }

    /// <summary>今日待跟的询盘数（终态不计）。</summary>
    public int DueTodayCount { get; set; }
}

/// <summary>
/// 统计聚合逻辑。写成纯函数（只依赖传入的列表和时刻），
/// 这样不用起数据库就能测，API 层只负责把数据喂进来。
/// </summary>
public static class StatsCalculator
{
    /// <summary>把一堆询盘聚合成统计结果。</summary>
    /// <param name="inquiries">要统计的全部询盘。</param>
    /// <param name="now">当前时刻，用于判断逾期/今日待跟。</param>
    public static StatsResult Compute(IEnumerable<Inquiry> inquiries, DateTime now)
    {
        StatsResult result = new StatsResult();

        // 先把所有分组补 0，前端渲染卡片时不用判断缺 key
        foreach (InquiryStatus status in Enum.GetValues<InquiryStatus>())
        {
            result.ByStatus[status] = 0;
        }
        foreach (IntentLevel intent in Enum.GetValues<IntentLevel>())
        {
            result.ByIntent[intent] = 0;
        }

        foreach (Inquiry inquiry in inquiries)
        {
            result.Total++;
            result.ByStatus[inquiry.Status]++;
            result.ByIntent[inquiry.Intent]++;

            DueKind dueKind = inquiry.GetDueKind(now);
            if (dueKind == DueKind.Overdue)
            {
                result.OverdueCount++;
            }
            if (dueKind == DueKind.DueToday)
            {
                result.DueTodayCount++;
            }
        }
        return result;
    }
}
