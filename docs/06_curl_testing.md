# HTTP URL 测试方案

集中验收脚本为 [scripts/curl_all_urls.sh](../scripts/curl_all_urls.sh)。它以当前 C# API 的实际 JSON 契约为准：请求字段采用 camelCase，响应字段采用 snake_case。

## 执行边界

本轮只交付测试方案和脚本，集中测试稍后执行。届时服务先启动，再运行：

~~~bash
BASE_URL=http://127.0.0.1:8080 bash scripts/curl_all_urls.sh
~~~

统一入口先运行隔离的 SQLite xUnit 集成测试；当 API 已按选定 provider 启动时，再启用 HTTP 冒烟：

~~~bash
bash scripts/run_concentrated_tests.sh
RUN_HTTP_SMOKE=1 BASE_URL=http://127.0.0.1:8080 bash scripts/run_concentrated_tests.sh
~~~

xUnit 套件中的 `RouteInventoryTests` 会从 ASP.NET Core Endpoint 元数据核对完整方法/路径集合；目录、身份交易、旧前台内容和负向场景分别由对应测试类覆盖。该套件可在 Windows、Linux、macOS 原生执行；bash 冒烟脚本用于已经启动的部署实例（Windows 可使用 Git Bash 或 WSL）。

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
| 旧前台内容 | activities、announcements、compat、captcha、compare、exchange、feed、gallery | 200，验证码一次性校验 |
| 旧前台互动 | messages、packages、tags、topics、votes、wholesale | 200/201，留言、标签和投票写入 |

这份脚本对应 docs/02_url_api_list.md 中所有具体 REST 路由；营销通配符已展开为三个当前支持的 kind。

## 第二阶段负向与并发方案

集中冒烟通过后，按数据库逐一执行以下场景：

- 未认证访问受保护资源应为 401；访问非归属地址、购物车或订单应为 404。
- 数量为 0、超库存、空购物车、无效配送/支付方式分别返回 400 或 409。
- 用旧 version 更新购物车必须返回 409；同一 idempotencyKey 只创建一张订单。
- 已取消或已支付订单不可二次取消；回调 provider 不存在为 404，重复流水号可安全重放。
- 验证码错误、过期或重复校验返回 400；比较列表为空、超过上限或包含下架商品分别返回 400/404。
- 未认证提交留言、标签或投票返回 401；无效投票选项返回 400，同一用户重复投票返回 409。
- 已过期的活动、专题、礼包和投票不出现在公开结果；相册及商品标签不得泄露下架商品。
- 每个列表用 page、page_size 测试默认值、1、100 和越界值。
- SQLite、PostgreSQL、MySQL 分别启动相同服务配置并执行脚本；每次都使用空库验证当前 EF 模型的创建与 URL 行为。

## 三数据库启动配置

SQLite 使用独立临时文件；PostgreSQL/MySQL 可通过部署 compose 启动依赖。每个 provider 均在一个空数据库上启动 API，再运行同一冒烟脚本：

~~~bash
Database__Provider=Sqlite Database__ConnectionString='Data Source=/tmp/ecshop-url-test.db' dotnet run --project src/EcShop.Api
Database__Provider=PostgreSql Database__ConnectionString='Host=127.0.0.1;Database=ecshop;Username=ecshop;Password=change-me-local-only' dotnet run --project src/EcShop.Api
Database__Provider=MySql Database__ConnectionString='Server=127.0.0.1;Database=ecshop;User=ecshop;Password=change-me-local-only' dotnet run --project src/EcShop.Api
~~~

当前启动器会在空数据库上按 EF 模型建表并写入基础验收数据。三次运行必须使用彼此隔离或已清理的数据源，避免账号、活动和库存状态互相污染。
