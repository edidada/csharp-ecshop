# 技术栈与库选择

选择 .NET 8 LTS（当前已在仓库以 `global.json` 固定 SDK patch）和 **ASP.NET Core Minimal APIs**。它是 .NET 官方 Web 框架，Kestrel 可在 Windows、Linux、macOS 运行；Minimal API 适合把 PHP 的大量小入口逐步组织为按业务边界划分的 route module。业务复杂到需要 MVC 特性时可在同一 host 增加 Controller，不需替换框架。

| 领域 | 选择 | 用途 |
| --- | --- | --- |
| Web/API | ASP.NET Core 8 | Kestrel、路由、DI、配置、健康检查、Problem Details |
| ORM/迁移 | EF Core 8 | `DbContext`、事务、映射、migration；复杂、性能敏感 SQL 可用参数化 ADO.NET/Dapper，但不进入 Handler |
| SQLite | `Microsoft.EntityFrameworkCore.Sqlite` | 开发、单机演示与 URL 级测试 |
| PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL 生产部署 |
| MySQL | `Pomelo.EntityFrameworkCore.MySql` | MySQL 8 与原 ECSHOP 表结构兼容 |
| 日志 | Serilog.AspNetCore | JSON/控制台结构化日志，携带 request id |
| API 描述 | Swashbuckle.AspNetCore | 开发环境 Swagger UI / OpenAPI 文档 |
| 测试 | xUnit + `Microsoft.AspNetCore.Mvc.Testing` | 从 HTTP 请求验证每个 URL 的状态码与 JSON 契约 |

依赖版本通过仓库根目录的 `Directory.Packages.props` 集中锁定，所有 EF Core provider 保持同一个 EF Core major（8）。不要在业务层引用任何具体 provider；数据库选择仅发生在 `EcShop.Infrastructure` 的启动装配。

## 数据库切换

`src/EcShop.Api/appsettings.json` 默认 SQLite：

```json
{"Database":{"Provider":"Sqlite","ConnectionString":"Data Source=ecshop.db"}}
```

PostgreSQL：

```json
{"Database":{"Provider":"PostgreSql","ConnectionString":"Host=127.0.0.1;Port=5432;Database=ecshop;Username=ecshop;Password=change-me"}}
```

MySQL 8：

```json
{"Database":{"Provider":"MySql","ConnectionString":"Server=127.0.0.1;Port=3306;Database=ecshop;User=ecshop;Password=change-me"}}
```

可用环境变量覆盖，例如 `Database__Provider=PostgreSql`。连接串、支付密钥和 JWT 密钥不得提交；生产用部署平台的 secret store 或环境变量提供。
