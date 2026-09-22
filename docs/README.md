# C# ECSHOP 迁移蓝图

本仓库将 `third_party/shopex-ecshop/upload/` 的 PHP ECSHOP 功能逐个迁移为可测试的 C# JSON API；不是逐行翻译。PHP 的 `xxx.php?act=...` / `flow.php?step=...` 入口与新 API 的映射和实施顺序见 [02_url_api_list.md](02_url_api_list.md)。

## 当前阶段：框架已建立，业务 URL 尚未开始

解决方案使用 .NET 8 LTS 和 ASP.NET Core。现已具备分层项目、数据库 provider 切换、结构化日志、Problem Details、OpenAPI、liveness/readiness 探针和 HTTP 集成测试骨架。除 `GET /healthz`、`GET /readyz` 外，URL 清单中的业务接口均未实现；审核通过后按 M1 起逐项实现并用 curl/集成测试验收、提交。

## 阅读顺序

1. [01_libraries.md](01_libraries.md)：框架、库与版本策略。
2. [07_platform_compatibility.md](07_platform_compatibility.md)：三平台构建与运行规则。
3. [02_url_api_list.md](02_url_api_list.md)：PHP 到 REST 的实施清单。
4. [04_architecture.md](04_architecture.md)：项目边界与依赖方向。
5. [05_detailed_design.md](05_detailed_design.md)：每个 URL 的实现约定。
6. [03_api_contract.md](03_api_contract.md)：目标 API 契约。
7. [06_curl_testing.md](06_curl_testing.md)：业务实现后的验收模板。
8. [deploy/README.md](deploy/README.md)：本地依赖与 Linux 部署模板。

## 项目结构

```text
src/
  EcShop.Api/             # ASP.NET Core host、路由、HTTP middleware
  EcShop.Application/     # 每个 URL 的 command/query 与 use case
  EcShop.Domain/          # 业务实体、值对象、规则与仓储接口
  EcShop.Infrastructure/  # EF Core、数据库实现、外部服务适配器
tests/
  EcShop.Api.Tests/       # 真实 HTTP 边界测试
```

## 构建

```bash
dotnet restore csharp-ecshop.sln
dotnet test csharp-ecshop.sln
dotnet run --project src/EcShop.Api
curl -i http://127.0.0.1:8080/healthz
# /readyz returns 503 until the selected database has been created and migrated.
```
