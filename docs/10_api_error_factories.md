# API 错误约定

参数校验和带说明的 404 使用 ASP.NET Core `ProblemDetails`；认证失败使用空的 401，简单资源不存在可使用空的 404；业务冲突返回带机器可读 `code` 的 JSON。未知异常由全局异常处理器处理，响应不得含 SQL、堆栈、密码、token、Cookie 或支付签名。

| 场景 | HTTP | `code` |
| --- | --- | --- |
| DTO/参数错误 | 400 | `validation_error` |
| 未登录 | 401 | 空响应 |
| 资源不存在、不可见或不归属 | 404 | 空响应或 `not_found` Problem Details |
| 乐观锁、重复写入、库存不足 | 409 | `version_conflict` / `already_voted` / `out_of_stock` |
| 不允许的订单状态 | 409 | `order_not_cancellable` / `order_not_payable` |

集中测试按上述状态码和稳定 `code` 验收；健康检查不使用 Problem Details。若后续引入统一错误工厂，应一次性更新代码、契约和全量 URL 脚本，避免同一错误产生两套格式。
