using InquiryDesk.Web.Domain;

namespace InquiryDesk.Tests;

/// <summary>
/// 状态流转规则测试：主线顺序流转放行、跳步/回退/终态再流转一律拒绝。
/// 规则本体在 Domain/Inquiry.ChangeStatus，这里只验证它的行为。
/// </summary>
public class StatusTransitionTests
{
    private static Inquiry CreateInquiry(InquiryStatus status)
    {
        return new Inquiry
        {
            CustomerName = "测试客户",
            Content = "测试内容",
            Status = status
        };
    }

    // 合法流转：主线顺序 + 任何非终态直达已流失
    [Theory]
    [InlineData(InquiryStatus.New, InquiryStatus.Contacted)]
    [InlineData(InquiryStatus.Contacted, InquiryStatus.FollowingUp)]
    [InlineData(InquiryStatus.FollowingUp, InquiryStatus.Won)]
    [InlineData(InquiryStatus.FollowingUp, InquiryStatus.Lost)]
    [InlineData(InquiryStatus.New, InquiryStatus.Lost)]
    [InlineData(InquiryStatus.Contacted, InquiryStatus.Lost)]
    public void AllowedTransition_ChangesStatus(InquiryStatus from, InquiryStatus to)
    {
        Inquiry inquiry = CreateInquiry(from);

        inquiry.ChangeStatus(to);

        Assert.Equal(to, inquiry.Status);
    }

    // 非法流转：跳步、回退、终态再动、同状态，都应抛异常且状态不变
    [Theory]
    [InlineData(InquiryStatus.New, InquiryStatus.FollowingUp)]
    [InlineData(InquiryStatus.New, InquiryStatus.Won)]
    [InlineData(InquiryStatus.Contacted, InquiryStatus.Won)]
    [InlineData(InquiryStatus.Contacted, InquiryStatus.New)]
    [InlineData(InquiryStatus.FollowingUp, InquiryStatus.Contacted)]
    [InlineData(InquiryStatus.Won, InquiryStatus.Contacted)]
    [InlineData(InquiryStatus.Won, InquiryStatus.Lost)]
    [InlineData(InquiryStatus.Lost, InquiryStatus.New)]
    [InlineData(InquiryStatus.Lost, InquiryStatus.FollowingUp)]
    [InlineData(InquiryStatus.New, InquiryStatus.New)]
    public void ForbiddenTransition_ThrowsAndKeepsStatus(InquiryStatus from, InquiryStatus to)
    {
        Inquiry inquiry = CreateInquiry(from);

        Assert.Throws<InvalidOperationException>(() => inquiry.ChangeStatus(to));

        Assert.Equal(from, inquiry.Status);
    }
}
