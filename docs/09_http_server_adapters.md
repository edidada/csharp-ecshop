# HTTP 路由模块

ASP.NET Core 是唯一 HTTP host，不再维护多个 server adapter。路由按 bounded context 放在 `EcShop.Api/Endpoints`：

```text
Endpoints/
  HealthEndpoints.cs
  CatalogEndpoints.cs       # 商品、分类、品牌、文章
  IdentityEndpoints.cs      # 注册、登录、地址
  CartEndpoints.cs
  OrderEndpoints.cs
  AdminEndpoints.cs
```

每个文件暴露 `Map<Context>Endpoints(this IEndpointRouteBuilder routes)`；`Program.cs` 仅负责注册 middleware 和装配模块。Endpoint 只翻译 HTTP，不能包含价格、库存、订单状态或 SQL 规则。公共响应格式、分页和错误映射应放入共享 API 组件，避免不同 URL 产生不一致的状态码。
