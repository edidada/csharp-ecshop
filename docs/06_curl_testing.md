# HTTP URL 测试方案

集中验收脚本为 [scripts/curl_all_urls.sh](../scripts/curl_all_urls.sh)。它以当前 C# API 的实际 JSON 契约为准：请求字段采用 camelCase，响应字段采用 snake_case。

## 执行边界

本轮只交付测试方案和脚本，集中测试稍后执行。届时服务先启动，再运行：

~~~bash
BASE_URL=http://127.0.0.1:8080 bash scripts/curl_all_urls.sh
~~~

要求本机具备 curl 与 jq。脚本自动创建唯一测试用户、不会打印 access token，并对每一个请求显式使用 curl 的 no-proxy 选项。支付成功订单会消耗一件演示库存，故应使用全新数据库，或在开始前保证商品 12 至少有 3 件可售库存。

## 正向 URL 覆盖

| 范围 | 覆盖路由/操作 | 验收点 |
| --- | --- | --- |
| 健康 | healthz、readyz | HTTP 200 |
| 公开目录 | home、分类、商品、价格报价、搜索、品牌、文章、区域 | HTTP 200、分页及关键字段 |
| 身份 | register、login、logout、GET/PATCH me | 201/200/204，注销后 token 返回 401 |
| 地址 | GET/POST/PATCH me/addresses | 新地址 ID、更新、列表 |
| 购物车 | GET/POST/PATCH/DELETE me/cart | 创建、版本更新、删除 |
| 结算 | options、quote | 配送选项及 order_amount |
| 订单 | create、同键重放、list、detail、cancel | 201/200、同订单 ID、取消状态 |
| 评论 | GET/POST goods comments | 201/200 |
| 支付 | mock callback 与重放 | accepted，第二次为 duplicate |
| 营销 | group-buy、auction、snatch 的 GET/POST | 201/200 |

这份脚本对应 docs/02_url_api_list.md 中所有具体 REST 路由；营销通配符已展开为三个当前支持的 kind。

## 第二阶段负向与并发方案

集中冒烟通过后，按数据库逐一执行以下场景：

- 未认证访问受保护资源应为 401；访问非归属地址、购物车或订单应为 404。
- 数量为 0、超库存、空购物车、无效配送/支付方式分别返回 400 或 409。
- 用旧 version 更新购物车必须返回 409；同一 idempotencyKey 只创建一张订单。
- 已取消或已支付订单不可二次取消；回调 provider 不存在为 404，重复流水号可安全重放。
- 每个列表用 page、page_size 测试默认值、1、100 和越界值。
- SQLite、PostgreSQL、MySQL 分别启动相同服务配置并执行脚本；后两者必须先完成迁移/建表，不能依赖 SQLite 的自动建表。
