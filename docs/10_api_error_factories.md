# API 错误约定

使用 ASP.NET Core `ProblemDetails`（RFC 9457）作为所有错误体，不创建另一套自定义异常响应格式。每个可预期业务错误映射为固定 status 和机器可读 `code`，并由全局异常处理器统一写入 `traceId`/request id；响应不得含 SQL、堆栈、密码、Cookie 或支付签名。

| 场景 | HTTP | `code` |
| --- | --- | --- |
| DTO/参数错误 | 400 | `validation_error` |
| 未登录 | 401 | `unauthenticated` |
| 无资源权限 | 403 | `forbidden` |
| 资源不存在或不可见 | 404 | `not_found` |
| 乐观锁、重复请求、库存不足 | 409 | `conflict` / `out_of_stock` |
| 不允许的订单状态 | 422 | `invalid_state` |

业务 URL 实现时为这些映射增加 HTTP 测试；当前健康检查不使用 Problem Details。
