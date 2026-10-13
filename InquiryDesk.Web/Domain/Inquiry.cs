namespace InquiryDesk.Web.Domain;

/// <summary>询盘来源。</summary>
public enum InquirySource
{
    Website,    // 网站
    Exhibition, // 展会
    Referral,   // 转介绍
    Phone,      // 电话
    Other       // 其他
}

/// <summary>意向等级。</summary>
public enum IntentLevel
{
    High,   // 高
    Medium, // 中
    Low     // 低
}

/// <summary>询盘状态，流转规则见 <see cref="Inquiry.ChangeStatus"/>。</summary>
public enum InquiryStatus
{
    New,         // 新询盘
    Contacted,   // 已联系
    FollowingUp, // 跟进中
    Won,         // 已成交（终态）
    Lost         // 已流失（终态）
}

/// <summary>到期分类，用于提醒列表和统计。</summary>
public enum DueKind
{
    None,     // 还没到期
    DueToday, // 今天要跟进
    Overdue   // 已逾期
}

/// <summary>
/// 一条客户询盘。状态流转和到期判断是核心业务规则，直接放在实体上，
/// 这样 API 层和测试都只面对一套规则，不会各写一份。
/// </summary>
public class Inquiry
{
    public int Id { get; set; }

    /// <summary>客户名（公司或联系人）。</summary>
    public string CustomerName { get; set; } = "";

    /// <summary>联系方式，电话/微信/邮箱任选，Demo 不做格式校验。</summary>
    public string ContactInfo { get; set; } = "";

    public InquirySource Source { get; set; }

    /// <summary>询盘内容原文。</summary>
    public string Content { get; set; } = "";

    public IntentLevel Intent { get; set; }

    public InquiryStatus Status { get; set; } = InquiryStatus.New;

    /// <summary>下次跟进时间，null 表示尚未安排。</summary>
    public DateTime? NextFollowUpAt { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>跟进记录，按时间先后追加。</summary>
    public List<FollowUpNote> Notes { get; set; } = new List<FollowUpNote>();

    /// <summary>是否为终态（已成交 / 已流失）。终态不再流转、不再产生到期提醒。</summary>
    public bool IsTerminal
    {
        get { return Status == InquiryStatus.Won || Status == InquiryStatus.Lost; }
    }

    /// <summary>
    /// 按业务规则变更状态。主线流转：新询盘 → 已联系 → 跟进中 → 已成交 / 已流失；
    /// 另外任何非终态都可以直接标记已流失，因为客户明确拒绝时没必要走完流程。
    /// </summary>
    /// <param name="newStatus">目标状态。</param>
    /// <exception cref="InvalidOperationException">流转不合法时抛出，消息可直接展示给用户。</exception>
    public void ChangeStatus(InquiryStatus newStatus)
    {
        if (IsTerminal)
        {
            throw new InvalidOperationException("已成交或已流失的询盘不能再变更状态。");
        }
        if (newStatus == Status)
        {
            throw new InvalidOperationException("新状态与当前状态相同，没有变化。");
        }
        if (!IsTransitionAllowed(Status, newStatus))
        {
            string fromText = EnumText.Status(Status);
            string toText = EnumText.Status(newStatus);
            throw new InvalidOperationException($"不能从「{fromText}」直接变更为「{toText}」，请按顺序流转。");
        }
        Status = newStatus;
    }

    /// <summary>判断一条状态流转是否合法（调用方已排除终态和同状态）。</summary>
    private static bool IsTransitionAllowed(InquiryStatus from, InquiryStatus to)
    {
        // 已流失从任何非终态都可直达，统一放行
        if (to == InquiryStatus.Lost)
        {
            return true;
        }

        switch (from)
        {
            case InquiryStatus.New:
                return to == InquiryStatus.Contacted;
            case InquiryStatus.Contacted:
                return to == InquiryStatus.FollowingUp;
            case InquiryStatus.FollowingUp:
                return to == InquiryStatus.Won;
            default:
                return false;
        }
    }

    /// <summary>
    /// 判断这条询盘相对给定时刻的到期情况。按自然日比较，不看时分秒：
    /// 跟进日期早于今天 = 已逾期，正好是今天 = 今日待跟。
    /// </summary>
    /// <param name="now">当前时刻，由调用方传入，方便测试。</param>
    public DueKind GetDueKind(DateTime now)
    {
        if (IsTerminal || NextFollowUpAt == null)
        {
            return DueKind.None;
        }

        DateTime dueDate = NextFollowUpAt.Value.Date;
        DateTime today = now.Date;
        if (dueDate < today)
        {
            return DueKind.Overdue;
        }
        if (dueDate == today)
        {
            return DueKind.DueToday;
        }
        return DueKind.None;
    }
}
