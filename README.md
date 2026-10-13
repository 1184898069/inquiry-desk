# 询盘跟进台 InquiryDesk

把散落在微信、电话、展会名片里的客户询盘，统一登记到一个台子上：谁问的、意向多强、跟进到哪一步、下次什么时候该联系——一页看清，逾期自动标红提醒。

一个可直接跑起来的 Demo：ASP.NET Core + SQLite + 原生前端，无构建步骤，`dotnet run` 即开。

## 功能

- **询盘登记**：客户名、联系方式、来源（网站/展会/转介绍/电话/其他）、询盘内容、意向等级（高/中/低）、下次跟进时间
- **状态流转**：新询盘 → 已联系 → 跟进中 → 已成交 / 已流失，按顺序流转，非法流转会被拒绝并给出原因；已成交、已流失为终态
- **跟进记录**：每条询盘可追加多条跟进备注，添加时可同时改下次跟进时间
- **到期提醒**：下次跟进时间已过且未成交/未流失的自动列入提醒区，逾期标红、今日待跟标橙
- **统计卡片**：总数、各状态计数、今日待跟数、已逾期数
- **筛选与导出**：列表按状态、意向筛选；一键导出 CSV（UTF-8 带 BOM，Excel 打开不乱码）
- **演示数据**：首次启动自动写入 8 条示例询盘（含 2 条逾期、1 条今日待跟），删掉 `inquirydesk.db` 重新启动即可重置

## 快速开始

环境：.NET 8 SDK。

```bash
cd InquiryDesk.Web
dotnet run
```

打开浏览器访问 http://localhost:5080

跑测试：

```bash
dotnet test
```

## 接口一览

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `/api/inquiries?status=&intent=` | 列表，可按状态、意向筛选（传枚举名） |
| POST | `/api/inquiries` | 新建询盘 |
| GET | `/api/inquiries/{id}` | 详情（含跟进记录） |
| PUT | `/api/inquiries/{id}/status` | 变更状态，非法流转返回 400 和原因 |
| POST | `/api/inquiries/{id}/notes` | 加跟进记录，可同时改下次跟进时间 |
| GET | `/api/inquiries/due` | 到期提醒（逾期在前） |
| GET | `/api/stats` | 统计计数 |
| GET | `/api/inquiries/export` | 导出 CSV |

## 项目结构

```
InquiryDesk.sln
├─ InquiryDesk.Web          Web 项目
│  ├─ Program.cs            启动：注册数据库、建表、写演示数据、挂接口
│  ├─ Domain/               领域层：实体与全部业务规则（不依赖 ASP.NET / EF）
│  │  ├─ Inquiry.cs         询盘实体：状态流转、到期判断都在这里
│  │  ├─ FollowUpNote.cs    跟进记录实体
│  │  ├─ StatsCalculator.cs 统计聚合（纯函数，好测）
│  │  └─ EnumText.cs        枚举 → 中文文本，集中一处
│  ├─ Data/
│  │  ├─ AppDbContext.cs    EF Core + SQLite 配置
│  │  └─ SeedData.cs        首次启动的演示数据
│  ├─ Api/
│  │  ├─ InquiryEndpoints.cs 接口定义：只做参数校验 + 调用领域规则
│  │  ├─ ApiModels.cs        请求/响应 DTO
│  │  └─ InquiryMapping.cs   实体 → DTO 映射
│  └─ wwwroot/              前端：index.html + app.js + style.css，原生实现
└─ InquiryDesk.Tests        xUnit 测试：状态流转、到期判断、统计聚合
```

## 关键设计说明

- **业务规则只写一份**：状态能不能流转、算不算逾期，都在 `Domain/Inquiry.cs` 里，API 层只负责把规则的拒绝原因转成 HTTP 400。改规则只改一处，测试也只测一处。
- **时间为参数传入**：到期判断和统计都接收一个 `now` 参数而不是内部读系统时钟，所以测试可以固定“今天”，不随日期漂移。
- **不做迁移**：Demo 用 `Database.EnsureCreated()` 建表，图的是 clone 下来就能跑；正式项目应换成 EF Core Migrations。
- **枚举存字符串**：数据库里状态、来源、意向都存英文名，肉眼可读；界面和 CSV 显示中文，走 `EnumText` 统一翻译。
- **前端零构建**：原生 HTML/JS/CSS + fetch，打开就能改，不需要 npm 工具链。

## 后续路线（未做，不画饼）

- CSV 批量导入历史询盘
- 邮箱自动汇入：定时拉取指定邮箱的新邮件生成询盘草稿
- 登录与权限：多销售各自的询盘归属与可见范围
- 提醒外发：逾期清单每日汇总推送到企业微信/邮件
