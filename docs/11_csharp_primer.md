# C# 新手入门：库依赖、关键字与语法（结合本项目）

本仓库是一个用 .NET 8 (C# 12) 把 PHP ECShop 重写成 JSON API 的项目。如果你刚接触 C#，这篇文档帮你用本项目真实的代码看懂：

1. 依赖是怎么来的、每个库干嘛用；
2. 代码里出现的 C# 关键字和现代语法是什么意思；
3. 从哪个文件开始读最顺。

阅读顺序建议：先读 docs/README.md 看整体蓝图，再回来看本篇抠语法细节。

---

## 1. 先认项目骨架

项目包含四个 src 项目通过项目引用串起来：Api 依赖 Application 加 Infrastructure，Infrastructure 依赖 Application 加 Domain。依赖方向单向，领域层不反向依赖 Web 层。

编译器与语言版本由根目录的 Directory.Build.props 决定：

- TargetFramework net8.0：跑在 .NET 8 运行时
- ImplicitUsings enable：自动引入常用命名空间，少写 using
- Nullable enable：开启可空引用类型检查
- TreatWarningsAsErrors true：警告即错误，逼你写干净

global.json 锁定 SDK 版本为 8.0.423，保证团队同版本编译器。

---

## 2. 库依赖从哪来

本项目的 NuGet 包版本集中在 Directory.Packages.props（集中包管理）：所有版本写在一个地方，各 csproj 只写用哪个包，不写版本。

各项目只声明用哪些包，例如 src/EcShop.Infrastructure/EcShop.Infrastructure.csproj：

- Microsoft.EntityFrameworkCore.Sqlite
- Npgsql.EntityFrameworkCore.PostgreSQL
- Pomelo.EntityFrameworkCore.MySql
- Microsoft.EntityFrameworkCore.Design

### 2.1 各库是干嘛的（对照本项目用法）

- Microsoft.EntityFrameworkCore.Sqlite / Npgsql / Pomelo.MySql：ORM，把 C# 类映射成数据库表，用 C# 写查询而不是 SQL。EcShopDbContext 继承 DbContext，实体类如 Goods 映射到 ecs_goods 表；docs/06_curl_testing.md 里同一套代码可切三种数据库。
- Serilog.AspNetCore：结构化日志。Program.cs 里 builder.Host.UseSerilog，控制台输出带级别的日志。
- Swashbuckle.AspNetCore：自动生成 Swagger/OpenAPI 文档。Program.cs 里开发环境 UseSwagger/UseSwaggerUI，浏览器能看到 API 清单。
- Microsoft.AspNetCore.Mvc.Testing：测试时把整个 Web 应用拉起来做真实 HTTP 测试。tests/EcShop.Api.Tests 的 ApiFactory 用它启动内存中的 API。
- xunit / Microsoft.NET.Test.Sdk：单元测试框架与运行器。tests 下 34 个测试即用它写，跑 dotnet test 即可。

小知识：为什么 ORM 是依赖？因为 EF Core 帮你做对象到表映射、变更追踪、SaveChanges 一键落库。本项目所有 db.Users.Add、await db.SaveChangesAsync(ct) 都来自它。

---

## 3. C# 关键字与语法（全部取自本项目）

### 3.1 namespace 与 using

namespace EcShop.Api.Endpoints 把类放进命名空间避免名字撞车。using Microsoft.EntityFrameworkCore 引入后可直接用 DbContext/EF 类型。本项目开启 ImplicitUsings，所以 System、System.Threading 等常用命名空间不用手写 using 就可用。

### 3.2 class 与 sealed

CatalogEntities.cs:5 的 Goods 类：

- public long GoodsId { get; set; } 是自动属性，编译器自动生成字段和 get/set
- public string GoodsName { get; set; } = string.Empty 给默认值避免 null
- public decimal ShopPrice { get; set; } 金额一律用 decimal，不用 float 防精度丢失
- public bool IsOnSale { get; set; } = true 布尔标志带默认

sealed 表示这个类不能再被继承。decimal 表示金额，本项目和文档都强调绝不用浮点。

### 3.3 record

IdentityEndpoints.cs:45：

public sealed record RegisterRequest(string? Username, string? Email, string? Password, bool AgreementAccepted);

record 是不可变数据载体，括号里是主构造函数参数，编译器自动生成只读属性和相等比较。HTTP 请求体反序列化后就是这种对象。string? 的问号表示这里允许为 null。

### 3.4 可空引用类型：问号、is null、is not null

因为开启 Nullable enable，编译器区分可能为 null 和一定不为 null：

- ShopUser? 返回值表示可能找不到用户
- request.Password?.Length 的问号是空条件访问，Password 为 null 时不会抛异常
- is null / is not null 比 == null 更安全

见 IdentityEndpoints.cs:38 的 CurrentUserAsync 与 :18 的注册校验。

### 3.5 async / await / Task / CancellationToken

Web API 大量 I/O，所以几乎每个处理方法都是 async 的。IdentityEndpoints.cs:15 的 RegisterAsync 用 await db.Users.AnyAsync 异步查询，await db.SaveChangesAsync(ct) 异步落库。CancellationToken ct 让调用方可以取消请求，数据库操作一路传下去。本项目所有数据库方法都接收它。

### 3.6 IResult 与 Results.*

- Results.Ok(new { id = user.UserId }) 返回 200 加 JSON
- Results.Created(路径, 对象) 返回 201 加 Location
- Results.Unauthorized 返回 401
- Results.Conflict 返回 409
- Results.Problem(statusCode: 400, ...) 返回 RFC 问题详情

Results.* 是最小 API 的返回工厂，配合 IResult 接口，路由方法签名统一为 Task<IResult>。

### 3.7 字符串插值与目标类型 new

return Results.Created($"/api/v1/me", new { id = user.UserId, access_token = ... });

美元符字符串在字符串里直接嵌表达式。new { ... } 是匿名对象，常用于拼一次性返回的 JSON（响应字段按文档是 snake_case，所以这里写 access_token 而非 AccessToken）。

### 3.8 switch 模式匹配加 or

EcShop.Infrastructure/DependencyInjection.cs:28 按数据库 provider 选择 EF 提供程序：

- case "sqlite" 用 UseSqlite
- case "postgresql" or "postgres" 用 UseNpgsql，or 模式一个分支匹配多值
- default 抛 InvalidOperationException，不静默吞错

### 3.9 集合表达式

LegacyContentEndpoints.cs:11：

private static readonly string[] CompatibilityCapabilities = ["catalog", "checkout", "content", "promotions"];

C# 12 允许用方括号直接构造数组，比 new[] 更简洁。

### 3.10 扩展方法与 static 类

IdentityEndpoints.cs:13：

public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder routes)

static class 加首参 this X 是扩展方法，让 routes.MapIdentityEndpoints() 看起来像自带方法。Program.cs 里正是这样拼装全部路由：app.MapIdentityEndpoints(); app.MapCatalogEndpoints(); ... 这种把路由拆成扩展方法是最小 API 常见组织方式。

### 3.11 LINQ 查询语法

IdentityEndpoints.cs:38：

from s in db.Sessions
join u in db.Users on s.UserId equals u.UserId
where s.TokenHash == Hash(token)
select u

除了查询语法，本项目更多用方法语法：db.Goods.Where(...).OrderBy(...).Select(...).ToListAsync(ct)。Where/OrderBy/Select/ToListAsync 都是 EF Core 提供的 LINQ 方法，最终翻译成 SQL。

### 3.12 依赖注入

EcShop.Infrastructure/DependencyInjection.cs:10 的 AddInfrastructure 用 services.AddDbContext 注册数据库上下文，并返回 services。Program.cs 调用 builder.Services.AddApplication() 和 AddInfrastructure(builder.Configuration) 完成装配。之后路由方法的参数 EcShopDbContext db、IMemoryCache cache 等，都是框架自动从容器注入的，业务代码从不需要自己 new。

### 3.13 最小 API 路由映射

LegacyContentEndpoints.cs 里 api.MapGet("/activities", ...)、api.MapPost("/captcha/verify", ...)、api.MapGet("/goods/{id:long}/gallery", ...)。MapGet/MapPost/MapPatch/MapDelete 把 HTTP 方法加路径绑定到一个方法。{id:long} 是路由参数加类型约束，非法输入在进入方法前就被拒绝。

---

## 4. 推荐的阅读路线

1. src/EcShop.Api/Program.cs 看应用怎么搭起来
2. src/EcShop.Api/Endpoints/IdentityEndpoints.cs 一个完整端点文件，涵盖 record 入参、async/await、LINQ、返回 IResult、扩展方法注册
3. src/EcShop.Infrastructure/Persistence/EcShopDbContext.cs 加 CatalogEntities.cs 看 C# 类如何映射成 ecs_* 表，金额用 decimal
4. tests/EcShop.Api.Tests/RouteInventoryTests.cs 看测试如何用代码核对全部 53 条路由都注册了

---

## 5. 新手容易踩的坑（本项目特有）

- 警告即错误：TreatWarningsAsErrors=true，没用到的变量、null 隐患都会编译失败。初学时会觉得老报错，但这是好事。
- 可空引用类型：开启后可能为 null 的地方要写问号或用 ?. ，否则编译器报警。
- 金额用 decimal 不用 double：浮点有精度问题，钱必须精确。
- 启动端口冲突：launchSettings.json 默认 8080，若被占用（如本机 nginx），Kestrel 会因合并绑定而崩溃报 10013。用 dotnet run --no-launch-profile 或指定 ASPNETCORE_URLS=http://127.0.0.1:8090 即可。
- 集中包管理：改版本去 Directory.Packages.props，不要在各 csproj 里写 Version=。

---

## 6. 一分钟跑起来

bash

dotnet restore csharp-ecshop.sln
dotnet test csharp-ecshop.sln
dotnet run --project src/EcShop.Api --no-launch-profile
curl -i http://127.0.0.1:8090/healthz

想看 API 全貌，开发环境启动后打开 Swagger UI（UseSwaggerUI 已启用）。
