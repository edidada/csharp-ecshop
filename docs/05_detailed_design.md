# URL 实现约定

已按 `docs/02_url_api_list.md` 的优先级完成具体 REST URL 编码。后续变更仍需从 PHP 入口追到所调用的 `includes/lib_*.php`、数据库表与模板数据，再将可观察行为转成 API 契约和测试；不把 PHP 的全局变量、`$_REQUEST`、拼接 SQL 或 MD5 密码复制进 C#。

## 一次请求的路径

1. `EcShop.Api/Endpoints/<Context>` 定义路由和 HTTP DTO，校验 path/query/body。
2. Endpoint 使用依赖注入的 `EcShopDbContext` 查询并编排短事务；库存、订单、支付等并发写入使用条件更新。
3. 多个 URL 共享的复杂工作流或领域规则抽取到 `EcShop.Application` / `EcShop.Domain`，而不是复制到多个 Endpoint。
4. `EcShop.Infrastructure` 集中 EF Core 映射、三种 provider 选择和数据库初始化；API 不包含 provider 分支。
5. API 将预期错误映射为 Problem Details；未知异常由全局异常处理中间件记录。

金额在领域层采用整数分或明确的 `decimal` 值对象，HTTP 输出始终为两位小数字符串。库存扣减、创建订单、取消订单、支付回调必须使用单一数据库事务和条件更新，禁止先查后无条件写入。

## PHP 迁移规则

| PHP 习惯 | C# 替代 |
| --- | --- |
| `$_REQUEST` | 明确读取 route、query、header 或 JSON body |
| `$db` / 拼接 SQL | 注入的 EF Core `DbContext` + LINQ/参数化 SQL |
| `$_SESSION` | Bearer token 哈希与数据库会话存储 |
| `show_message()` / 跳转 | 明确 HTTP status + Problem Details/JSON |
| 全局 `$_CFG` | `IOptions<T>` 和环境变量 |
| MD5 密码 | 现代、可升级的密码哈希服务 |
