using InquiryDesk.Web.Domain;

namespace InquiryDesk.Web.Data;

/// <summary>
/// 演示数据：首次启动、表为空时写入 8 条询盘，覆盖各状态和意向等级，
/// 其中 2 条已逾期、1 条今日待跟，方便一打开页面就能看到提醒区效果。
/// 日期都相对“当前时刻”计算，保证任何时候第一次运行演示效果都成立。
/// </summary>
public static class SeedData
{
    /// <summary>表里已有数据时什么都不做，避免每次启动重复塞数据。</summary>
    public static void SeedIfEmpty(AppDbContext db)
    {
        if (db.Inquiries.Any())
        {
            return;
        }

        DateTime now = DateTime.Now;
        List<Inquiry> samples = BuildSamples(now);
        db.Inquiries.AddRange(samples);
        db.SaveChanges();
    }

    private static List<Inquiry> BuildSamples(DateTime now)
    {
        List<Inquiry> samples = new List<Inquiry>();

        // 1：跟进中、高意向，已逾期 3 天（演示逾期提醒）
        Inquiry zhang = new Inquiry
        {
            CustomerName = "宏远机械 张建国",
            ContactInfo = "138-XXXX-2046",
            Source = InquirySource.Website,
            Content = "想采购一批工业传感器，先要 200 个试单，合适的话按月续订。",
            Intent = IntentLevel.High,
            Status = InquiryStatus.FollowingUp,
            NextFollowUpAt = now.AddDays(-3),
            CreatedAt = now.AddDays(-10)
        };
        zhang.Notes.Add(new FollowUpNote
        {
            Content = "已发报价单和样品参数，等对方确认交期。",
            CreatedAt = now.AddDays(-6)
        });
        zhang.Notes.Add(new FollowUpNote
        {
            Content = "电话沟通过一次，对方说要等采购主管回来再定。",
            CreatedAt = now.AddDays(-4)
        });
        samples.Add(zhang);

        // 2：已联系、中意向，已逾期 1 天（演示逾期提醒）
        Inquiry li = new Inquiry
        {
            CustomerName = "李小雨",
            ContactInfo = "微信 lixiaoyu_home",
            Source = InquirySource.Exhibition,
            Content = "在展会上看了智能家居整套方案，想先给自家新房做一套预算。",
            Intent = IntentLevel.Medium,
            Status = InquiryStatus.Contacted,
            NextFollowUpAt = now.AddDays(-1),
            CreatedAt = now.AddDays(-5)
        };
        li.Notes.Add(new FollowUpNote
        {
            Content = "已加微信，发了户型图，等她确认量房时间。",
            CreatedAt = now.AddDays(-3)
        });
        samples.Add(li);

        // 3：新询盘、高意向，今天要跟进（演示今日待跟）
        Inquiry wang = new Inquiry
        {
            CustomerName = "王强",
            ContactInfo = "139-XXXX-7781",
            Source = InquirySource.Phone,
            Content = "来电咨询定制包装盒，5000 个起订，急着要打样报价。",
            Intent = IntentLevel.High,
            Status = InquiryStatus.New,
            NextFollowUpAt = now.Date.AddHours(17),
            CreatedAt = now.AddDays(-1)
        };
        samples.Add(wang);

        // 4：跟进中、中意向，未到期
        Inquiry chen = new Inquiry
        {
            CustomerName = "陈美玲",
            ContactInfo = "chenml@example.com",
            Source = InquirySource.Referral,
            Content = "老客户介绍来的，想了解区域代理加盟政策和首批进货折扣。",
            Intent = IntentLevel.Medium,
            Status = InquiryStatus.FollowingUp,
            NextFollowUpAt = now.AddDays(2),
            CreatedAt = now.AddDays(-7)
        };
        chen.Notes.Add(new FollowUpNote
        {
            Content = "已寄加盟政策手册，约了下周视频会细聊。",
            CreatedAt = now.AddDays(-2)
        });
        samples.Add(chen);

        // 5：新询盘、低意向，未到期
        Inquiry liu = new Inquiry
        {
            CustomerName = "刘洋",
            ContactInfo = "liuyang@office.example.com",
            Source = InquirySource.Website,
            Content = "公司要批量采购办公电脑，先询个价，对比几家再说。",
            Intent = IntentLevel.Low,
            Status = InquiryStatus.New,
            NextFollowUpAt = now.AddDays(5),
            CreatedAt = now.AddDays(-2)
        };
        samples.Add(liu);

        // 6：已成交（终态）
        Inquiry zhao = new Inquiry
        {
            CustomerName = "赵雪",
            ContactInfo = "137-XXXX-3310",
            Source = InquirySource.Exhibition,
            Content = "展会现场签了年度服务合同，首付款已到账。",
            Intent = IntentLevel.High,
            Status = InquiryStatus.Won,
            NextFollowUpAt = null,
            CreatedAt = now.AddDays(-20)
        };
        zhao.Notes.Add(new FollowUpNote
        {
            Content = "合同已归档，转交付同事对接实施。",
            CreatedAt = now.AddDays(-18)
        });
        samples.Add(zhao);

        // 7：已流失（终态）
        Inquiry sun = new Inquiry
        {
            CustomerName = "孙磊",
            ContactInfo = "sunlei@example.com",
            Source = InquirySource.Other,
            Content = "问完价格后明确表示预算不够，暂时不考虑了。",
            Intent = IntentLevel.Low,
            Status = InquiryStatus.Lost,
            NextFollowUpAt = null,
            CreatedAt = now.AddDays(-15)
        };
        samples.Add(sun);

        // 8：已联系、中意向，明天跟进
        Inquiry zhou = new Inquiry
        {
            CustomerName = "周婷",
            ContactInfo = "微信 zhouting_trade",
            Source = InquirySource.Website,
            Content = "咨询产品出口认证事宜，问我们有没有 CE 和 RoHS 的现成报告。",
            Intent = IntentLevel.Medium,
            Status = InquiryStatus.Contacted,
            NextFollowUpAt = now.AddDays(1),
            CreatedAt = now.AddDays(-3)
        };
        samples.Add(zhou);

        return samples;
    }
}
