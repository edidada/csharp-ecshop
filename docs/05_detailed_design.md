# URL 实现约定

后续按 `docs/02_url_api_list.md` 的优先级逐项实现。每一项从 PHP 入口追到所调用的 `includes/lib_*.php`、数据库表与模板数据，再将可观察行为转成 API 契约和测试；不把 PHP 的全局变量、`$_REQUEST`、拼接 SQL 或 MD5 密码复制进 C#。

## 一次请求的路径

1. `EcShop.Api/Endpoints/<Context>` 定义路由和 HTTP DTO，校验 path/query/body。
2. Endpoint 调用 `EcShop.Application` 内仅负责一个动作的 command/query handler。
3. Handler 使用 Domain 规则和仓储接口，短事务由 Application 侧控制。
4. `EcShop.Infrastructure` 以 EF Core 或参数化 SQL 访问数据库。
5. API 将预期错误映射为 RFC 9457 Problem Details；未知异常由全局异常处理中间件记录。

金额在领域层采用整数分或明确的 `decimal` 值对象，HTTP 输出始终为两位小数字符串。库存扣减、创建订单、取消订单、支付回调必须使用单一数据库事务和条件更新，禁止先查后无条件写入。

## PHP 迁移规则

| PHP 习惯 | C# 替代 |
| --- | --- |
| `$_REQUEST` | 明确读取 route、query、header 或 JSON body |
| `$db` / 拼接 SQL | repository + LINQ 或参数化 SQL |
| `$_SESSION` | 认证 middleware 的 `ClaimsPrincipal` 与会话存储 |
| `show_message()` / 跳转 | 明确 HTTP status + Problem Details/JSON |
| 全局 `$_CFG` | `IOptions<T>` 和环境变量 |
| MD5 密码 | 现代、可升级的密码哈希服务 |
