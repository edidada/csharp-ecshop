# HTTP 路由模块

ASP.NET Core 是唯一 HTTP host，不再维护多个 server adapter。路由按 bounded context 放在 `EcShop.Api/Endpoints`：

```text
Endpoints/
  CatalogEndpoints.cs       # 商品、分类、品牌、文章
  IdentityEndpoints.cs      # 注册、登录、地址
  CartEndpoints.cs
  CheckoutEndpoints.cs
  OrderEndpoints.cs
  CommentEndpoints.cs
  PaymentEndpoints.cs
  PromotionEndpoints.cs
  LegacyContentEndpoints.cs # 旧前台内容、互动与兼容入口
```

健康与就绪探针直接在 `Program.cs` 映射，其余文件暴露 `Map<Context>Endpoints(this IEndpointRouteBuilder routes)`。Endpoint 是当前垂直切片边界，负责 HTTP、授权和短事务编排；不拼接 SQL，也不根据具体数据库 provider 分支。多个模块共享的复杂规则再抽取到 Application/Domain。公共响应、分页和错误映射后续应逐步收敛到共享 API 组件。
