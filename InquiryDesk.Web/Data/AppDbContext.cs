using InquiryDesk.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace InquiryDesk.Web.Data;

/// <summary>
/// 数据库上下文。Demo 用 SQLite 单文件库，启动时 EnsureCreated 建表，不做迁移。
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Inquiry> Inquiries { get; set; }

    public DbSet<FollowUpNote> FollowUpNotes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 枚举存成字符串：数据库里肉眼可读，排查问题时不用再查映射表
        modelBuilder.Entity<Inquiry>()
            .Property(inquiry => inquiry.Source)
            .HasConversion<string>();
        modelBuilder.Entity<Inquiry>()
            .Property(inquiry => inquiry.Intent)
            .HasConversion<string>();
        modelBuilder.Entity<Inquiry>()
            .Property(inquiry => inquiry.Status)
            .HasConversion<string>();

        // 一条询盘多条跟进记录；删询盘时连带删记录（Demo 不提供删除接口，这里只是把关系定义完整）
        modelBuilder.Entity<Inquiry>()
            .HasMany(inquiry => inquiry.Notes)
            .WithOne(note => note.Inquiry)
            .HasForeignKey(note => note.InquiryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
