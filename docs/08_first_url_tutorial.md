# 第一个业务 URL（审核后开始）

业务 URL 尚未实现。审核通过后的第一个目标是 `GET /api/v1/home` 或 `GET /api/v1/goods/{id}`；推荐先实现商品详情，因为它对应 PHP `goods.php?id=` 且是只读路径。

每一个 URL 按此顺序完成：

1. 从 PHP 入口和其调用的 library 中记录输入、可见性规则、表读取和响应含义。
2. 在 `docs/03_api_contract.md` 固化请求、响应与错误码。
3. 添加 Domain 规则、Application query/use case、Infrastructure 映射和 API endpoint。
4. 为 SQLite 编写独立测试数据，再用 `WebApplicationFactory` 验证 HTTP 契约。
5. 添加可复制 curl 脚本，并对 PostgreSQL、MySQL 跑同一契约测试。
6. 只提交这一 URL 及其 migration、测试与文档。
