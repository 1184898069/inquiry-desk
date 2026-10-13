using InquiryDesk.Web.Domain;

namespace InquiryDesk.Tests;

/// <summary>
/// 统计聚合测试：总数、按状态/意向分组计数、逾期与今日待跟计数。
/// StatsCalculator 是纯函数，喂固定数据和固定时间即可验证，不需要数据库。
/// </summary>
public class StatsCalculatorTests
{
    private static readonly DateTime Now = new DateTime(2026, 10, 13, 12, 0, 0);

    private static Inquiry CreateInquiry(InquiryStatus status, IntentLevel intent, DateTime? nextFollowUpAt)
    {
        return new Inquiry
        {
            CustomerName = "测试客户",
            Content = "测试内容",
            Status = status,
            Intent = intent,
            NextFollowUpAt = nextFollowUpAt
        };
    }

    [Fact]
    public void Compute_CountsEverything()
    {
        DateTime yesterday = new DateTime(2026, 10, 12, 9, 0, 0);
        DateTime today = new DateTime(2026, 10, 13, 18, 0, 0);
        DateTime tomorrow = new DateTime(2026, 10, 14, 9, 0, 0);

        List<Inquiry> inquiries = new List<Inquiry>
        {
            CreateInquiry(InquiryStatus.New, IntentLevel.High, yesterday),          // 逾期
            CreateInquiry(InquiryStatus.Contacted, IntentLevel.Medium, today),      // 今日待跟
            CreateInquiry(InquiryStatus.FollowingUp, IntentLevel.High, tomorrow),   // 未到期
            CreateInquiry(InquiryStatus.Won, IntentLevel.Low, yesterday),           // 终态，不算逾期
            CreateInquiry(InquiryStatus.Lost, IntentLevel.Medium, null),            // 终态，未安排
            CreateInquiry(InquiryStatus.New, IntentLevel.Low, yesterday)            // 逾期
        };

        StatsResult stats = StatsCalculator.Compute(inquiries, Now);

        Assert.Equal(6, stats.Total);
        Assert.Equal(2, stats.ByStatus[InquiryStatus.New]);
        Assert.Equal(1, stats.ByStatus[InquiryStatus.Contacted]);
        Assert.Equal(1, stats.ByStatus[InquiryStatus.FollowingUp]);
        Assert.Equal(1, stats.ByStatus[InquiryStatus.Won]);
        Assert.Equal(1, stats.ByStatus[InquiryStatus.Lost]);
        Assert.Equal(2, stats.ByIntent[IntentLevel.High]);
        Assert.Equal(2, stats.ByIntent[IntentLevel.Medium]);
        Assert.Equal(2, stats.ByIntent[IntentLevel.Low]);
        Assert.Equal(2, stats.OverdueCount);
        Assert.Equal(1, stats.DueTodayCount);
    }

    [Fact]
    public void Compute_EmptyList_ReturnsZerosWithAllGroupsPresent()
    {
        StatsResult stats = StatsCalculator.Compute(new List<Inquiry>(), Now);

        Assert.Equal(0, stats.Total);
        Assert.Equal(0, stats.OverdueCount);
        Assert.Equal(0, stats.DueTodayCount);
        // 所有分组都要有 key（值为 0），前端渲染卡片时不用判空
        Assert.Equal(Enum.GetValues<InquiryStatus>().Length, stats.ByStatus.Count);
        Assert.Equal(Enum.GetValues<IntentLevel>().Length, stats.ByIntent.Count);
        Assert.All(stats.ByStatus.Values, count => Assert.Equal(0, count));
        Assert.All(stats.ByIntent.Values, count => Assert.Equal(0, count));
    }
}
