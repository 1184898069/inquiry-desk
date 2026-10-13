using InquiryDesk.Web.Api;
using InquiryDesk.Web.Data;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// SQLite 文件就放在项目目录下，一个文件就是整个数据库，备份/删除重来都方便
string dbPath = Path.Combine(builder.Environment.ContentRootPath, "inquirydesk.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

WebApplication app = builder.Build();

// Demo 用 EnsureCreated 建表而不做迁移，降低上手门槛；表为空时写入演示数据
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedData.SeedIfEmpty(db);
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapInquiryEndpoints();

app.Run();
