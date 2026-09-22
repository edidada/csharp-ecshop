# macOS、Linux、Windows 兼容性

所有项目目标为 `net8.0`，不包含 OS 专属 API；Kestrel、EF Core 和三个数据库 provider 均支持三平台。开发和 CI 使用相同命令：

```bash
dotnet restore csharp-ecshop.sln
dotnet build csharp-ecshop.sln --no-restore
dotnet test csharp-ecshop.sln --no-build
```

| 项目 | macOS | Linux | Windows |
| --- | --- | --- | --- |
| SDK | .NET 8 SDK | .NET 8 SDK | .NET 8 SDK |
| HTTP | Kestrel | Kestrel + Nginx | Kestrel（可配 IIS/Nginx） |
| SQLite | Microsoft.Data.Sqlite provider | 同左 | 同左 |
| PostgreSQL/MySQL | 容器或本机服务 | 容器或本机服务 | Docker Desktop/本机服务 |
| 测试 | `dotnet test` | `dotnet test` | `dotnet test` |

CI 应至少覆盖 `macos-latest`、`ubuntu-latest`、`windows-latest`，并在 Linux job 用 Docker 对 PostgreSQL、MySQL 运行迁移和集成测试。生产运行时选 `.NET 8 ASP.NET Core Runtime`，不是 SDK。
