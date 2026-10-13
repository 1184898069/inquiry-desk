namespace InquiryDesk.Web.Domain;

/// <summary>
/// 把枚举翻译成界面和导出文件用的中文文本。集中放一处，
/// 避免前端、CSV、报错消息各写一份导致不一致。
/// </summary>
public static class EnumText
{
    /// <summary>状态的中文名。</summary>
    public static string Status(InquiryStatus status)
    {
        switch (status)
        {
            case InquiryStatus.New:
                return "新询盘";
            case InquiryStatus.Contacted:
                return "已联系";
            case InquiryStatus.FollowingUp:
                return "跟进中";
            case InquiryStatus.Won:
                return "已成交";
            case InquiryStatus.Lost:
                return "已流失";
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "未知状态");
        }
    }

    /// <summary>来源的中文名。</summary>
    public static string Source(InquirySource source)
    {
        switch (source)
        {
            case InquirySource.Website:
                return "网站";
            case InquirySource.Exhibition:
                return "展会";
            case InquirySource.Referral:
                return "转介绍";
            case InquirySource.Phone:
                return "电话";
            case InquirySource.Other:
                return "其他";
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source, "未知来源");
        }
    }

    /// <summary>意向等级的中文名。</summary>
    public static string Intent(IntentLevel intent)
    {
        switch (intent)
        {
            case IntentLevel.High:
                return "高";
            case IntentLevel.Medium:
                return "中";
            case IntentLevel.Low:
                return "低";
            default:
                throw new ArgumentOutOfRangeException(nameof(intent), intent, "未知意向等级");
        }
    }
}
