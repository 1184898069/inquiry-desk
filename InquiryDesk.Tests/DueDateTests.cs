using InquiryDesk.Web.Domain;

namespace InquiryDesk.Tests;

/// <summary>
/// 到期判断测试：按自然日比较——早于今天 = 已逾期，正好今天 = 今日待跟；
/// 终态和未安排时间的询盘永远不算到期。“当前时间”固定写死，保证测试不随日期漂移。
/// </summary>
public class DueDateTests
{
    // 固定一个“今天”：2026-10-13 下午
    private static readonly DateTime Now = new DateTime(2026, 10, 13, 15, 30, 0);

    private static Inquiry CreateInquiry(InquiryStatus status, DateTime? nextFollowUpAt)
    {
        return new Inquiry
        {
            CustomerName = "测试客户",
            Content = "测试内容",
            Status = status,
            NextFollowUpAt = nextFollowUpAt
        };
    }

    [Fact]
    public void FollowUpYesterday_IsOverdue()
    {
        Inquiry inquiry = CreateInquiry(InquiryStatus.FollowingUp, new DateTime(2026, 10, 12, 9, 0, 0));

        Assert.Equal(DueKind.Overdue, inquiry.GetDueKind(Now));
    }

    [Fact]
    public void FollowUpEarlierToday_IsDueTodayNotOverdue()
    {
        // 同一天但时刻已过：按自然日算仍是今日待跟，不能误报逾期
        Inquiry inquiry = CreateInquiry(InquiryStatus.Contacted, new DateTime(2026, 10, 13, 8, 0, 0));

        Assert.Equal(DueKind.DueToday, inquiry.GetDueKind(Now));
    }

    [Fact]
    public void FollowUpLaterToday_IsDueToday()
    {
        Inquiry inquiry = CreateInquiry(InquiryStatus.New, new DateTime(2026, 10, 13, 23, 59, 0));

        Assert.Equal(DueKind.DueToday, inquiry.GetDueKind(Now));
    }

    [Fact]
    public void FollowUpTomorrow_IsNone()
    {
        Inquiry inquiry = CreateInquiry(InquiryStatus.New, new DateTime(2026, 10, 14, 9, 0, 0));

        Assert.Equal(DueKind.None, inquiry.GetDueKind(Now));
    }

    [Fact]
    public void NoFollowUpPlanned_IsNone()
    {
        Inquiry inquiry = CreateInquiry(InquiryStatus.New, null);

        Assert.Equal(DueKind.None, inquiry.GetDueKind(Now));
    }

    [Fact]
    public void WonInquiryWithPastDate_IsNone()
    {
        // 已成交是终态：哪怕跟进时间早已过去，也不再提醒
        Inquiry inquiry = CreateInquiry(InquiryStatus.Won, new DateTime(2026, 10, 1, 9, 0, 0));

        Assert.Equal(DueKind.None, inquiry.GetDueKind(Now));
    }

    [Fact]
    public void LostInquiryWithPastDate_IsNone()
    {
        Inquiry inquiry = CreateInquiry(InquiryStatus.Lost, new DateTime(2026, 10, 1, 9, 0, 0));

        Assert.Equal(DueKind.None, inquiry.GetDueKind(Now));
    }
}
