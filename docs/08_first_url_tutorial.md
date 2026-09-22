# URL 实现与验收工作流

具体 REST URL 已完成编码。后续修改任何 URL 时，保持以下顺序：

1. 对照 PHP 入口及其 library，确认输入、可见性规则、数据库读写和状态变化。
2. 在 [API 契约](03_api_contract.md) 更新请求、响应、错误码和鉴权约束。
3. 同步修改实体映射、endpoint 与三 provider 可执行的持久化逻辑。
4. 在独立 SQLite 测试库中补充 `WebApplicationFactory` 契约测试，测试不可依赖执行顺序或上次运行残留数据。
5. 将正向主链路加入 [全 URL 冒烟脚本](../scripts/curl_all_urls.sh)，将错误、并发和幂等场景加入 [集中测试方案](06_curl_testing.md)。
6. SQLite、PostgreSQL、MySQL 使用同一 HTTP 脚本集中验收，结果通过后再提交。

商品详情 `GET /api/v1/goods/{id}` 可作为新环境的最小业务验证：商品 12 应为 200，下架商品 13 应为 404；健康和就绪探针不等价于业务表已正确映射。
