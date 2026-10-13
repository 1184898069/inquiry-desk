namespace InquiryDesk.Web.Domain;

/// <summary>一条跟进记录：销售每次联系客户后留下的备注。</summary>
public class FollowUpNote
{
    public int Id { get; set; }

    /// <summary>所属询盘的编号。</summary>
    public int InquiryId { get; set; }

    /// <summary>所属询盘（EF 导航属性）。</summary>
    public Inquiry? Inquiry { get; set; }

    /// <summary>备注内容。</summary>
    public string Content { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}
