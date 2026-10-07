# Provider 数据源调研（Windows / C# 实现参考）

> **来源与许可声明**
> 本文内容整理自两个 MIT 许可的开源项目：
> - [nesszer/Win-CodexBar](https://github.com/nesszer/Win-CodexBar)（Rust + Tauri，**主要依据**，下文链接中的 `WCB` 即此仓库 `rust/src/`）
> - [steipete/CodexBar](https://github.com/steipete/CodexBar)（Swift，次要参照）
>
> 两个项目均为 MIT License，Copyright 归各自作者。Tokenbar 如移植其中的逻辑或常量，应在仓库 `THIRD_PARTY_NOTICES` / About 页保留其版权与许可声明。
>
> 每个 provider 使用的具体源文件（`WCB` = `https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/`）：
>
> | Provider | 源文件 |
> |---|---|
> | Codex | [providers/codex/mod.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/codex/mod.rs)、[codex/api.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/codex/api.rs)、[codex/pat.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/codex/pat.rs)、[codex/subscription.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/codex/subscription.rs)、[codex_accounts/api.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/codex_accounts/api.rs)、[codex_accounts/credentials.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/codex_accounts/credentials.rs) |
> | Claude Code | [providers/claude/mod.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/claude/mod.rs)、[claude/oauth/mod.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/claude/oauth/mod.rs)、[claude/oauth/credentials_store.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/claude/oauth/credentials_store.rs)、[claude/oauth/refresh.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/claude/oauth/refresh.rs)、[claude/scoped_weekly.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/claude/scoped_weekly.rs)、[claude/web_api.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/claude/web_api.rs)、[cost_scanner/claude_roots.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/cost_scanner/claude_roots.rs) |
> | Cursor | [providers/cursor/mod.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/cursor/mod.rs)、[cursor/api.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/cursor/api.rs)、[cursor/app_auth.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/cursor/app_auth.rs)、[cursor/token_cost.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/cursor/token_cost.rs)、[cursor/team_budget.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/cursor/team_budget.rs) |
> | Gemini CLI | [providers/gemini/mod.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/gemini/mod.rs)、[gemini/api.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/gemini/api.rs)；CodexBar [docs/gemini.md](https://github.com/steipete/CodexBar/blob/HEAD/docs/gemini.md) |
> | Antigravity | [providers/antigravity/mod.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/antigravity/mod.rs)、[antigravity/quota_summary.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/antigravity/quota_summary.rs)、[antigravity/legacy_status.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/antigravity/legacy_status.rs)、[antigravity/cli_fallback.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/antigravity/cli_fallback.rs)、[antigravity/cli_resolution.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/antigravity/cli_resolution.rs) |
> | OpenCode Zen | [providers/opencode/mod.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/opencode/mod.rs)、[opencode/billing.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/opencode/billing.rs)、[opencode/scraper.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/opencode/scraper.rs) |
> | OpenCode Go | [providers/opencodego/mod.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/opencodego/mod.rs)、[opencodego/usage_api.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/opencodego/usage_api.rs)、[opencodego/console.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/opencodego/console.rs)、[opencodego/local.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/opencodego/local.rs)、[opencodego/legacy.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/opencodego/legacy.rs)、[opencodego/tokens.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/opencodego/tokens.rs) |
> | Grok / xAI | [providers/grok/mod.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/grok/mod.rs)、[grok/credits_proxy.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/grok/credits_proxy.rs)、[grok/accounts.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/grok/accounts.rs)、[grok/accounts/refresh.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/grok/accounts/refresh.rs)、[grok/billing/mod.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/grok/billing/mod.rs)、[providers/xai/mod.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/xai/mod.rs) |
> | OpenRouter | [providers/openrouter/mod.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/openrouter/mod.rs)、[openrouter/activity.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/providers/openrouter/activity.rs) |
> | 共用：浏览器 Cookie | [browser/cookies.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/browser/cookies.rs)、[browser/detection.rs](https://github.com/nesszer/Win-CodexBar/blob/HEAD/rust/src/browser/detection.rs) |
>
> 调研时间：2026-10-07，读取的是两个仓库默认分支 HEAD。所有私有/未公开接口都可能随服务端改版失效。凡是**没在源码里直接看到**的内容标注「未核实」。

---

## 0. 共用模型与基础设施

### 0.1 统一的数据结构（建议 C# 照搬）

Win-CodexBar 每个 provider 输出同一套结构，Tokenbar 建议对应定义：

| Rust 类型 | 字段 | 含义 |
|---|---|---|
| `RateWindow` | `used_percent: f64`（0–100）、`window_minutes: Option<u32>`、`resets_at: Option<DateTime<Utc>>`、`reset_description: Option<String>`、`is_informational` | 一个用量窗口（进度条） |
| `UsageSnapshot` | `primary`、`secondary`、`tertiary`、`extra_rate_windows[{id,label,window}]`、`primary_label`、`login_method`（套餐名）、`account_email`、`account_organization` | 一次抓取结果 |
| `CostSnapshot` | `used`、`limit`、`currency`、`period`、`balance`、`daily[{day, amount}]` | 金额/余额 |
| `ProviderInventoryItem` | `id`、`title`、`available_count`、`next_expires_at` | 可用的「重置券」等库存 |
| `SourceMode` | `Auto` / `OAuth` / `Web` / `Cli` | 数据来源选择 |

**窗口角色按时长判定，不按 JSON 中的位置**：300 分钟 = 5 小时会话窗、10080 分钟 = 周窗、约 43200 分钟 = 月窗。

### 0.2 Windows 上读取 Chromium 浏览器 Cookie（Cursor / Claude Web / OpenCode / Grok Web 用）

来源：`browser/cookies.rs`、`browser/detection.rs`。

1. **浏览器根目录**（`%LOCALAPPDATA%` 下）：
   - Chrome `Google\Chrome\User Data`；Chrome Beta/Dev/Canary(`SxS`) 同级目录
   - Edge `Microsoft\Edge\User Data`
   - Brave `BraveSoftware\Brave-Browser\User Data`
   - Chromium `Chromium\User Data`、Arc 等
   - 遍历 `Default`、`Profile N` 子目录；Cookie 库在 `<profile>\Network\Cookies`（旧版在 `<profile>\Cookies`）。
2. **主密钥**：读 `User Data\Local State`（JSON）→ `os_crypt.encrypted_key`（base64）→ 解码后去掉前 5 字节 `"DPAPI"` → `CryptUnprotectData`（C#：`System.Security.Cryptography.ProtectedData.Unprotect(..., DataProtectionScope.CurrentUser)`）得到 32 字节 AES key。
3. **复制数据库再读**：浏览器运行时会锁住 Cookies 文件，先复制到临时目录（连同 `-wal`/`-journal`），用 `Microsoft.Data.Sqlite` 只读打开。
4. **SQL**：
   ```sql
   SELECT name, encrypted_value, host_key, path, expires_utc, is_secure, is_httponly
   FROM cookies WHERE host_key LIKE '%domain' OR host_key LIKE '.domain'
   ```
5. **解密 `encrypted_value`**：
   - 前缀 `v10` / `v11`：AES-256-GCM。结构 = 3 字节前缀 + 12 字节 nonce + 密文 + 16 字节 tag（C#：`AesGcm`）。
   - 新版数据库（`meta` 表 version ≥ 24）明文前 32 字节是 `SHA256(host_key)`，需要跳过。
   - 前缀 `v20`：**App-Bound Encryption（ABE）**，Chrome 127+ 默认开启，普通进程**无法解密**。Win-CodexBar 的处理是：放弃、提示用户手动粘贴 Cookie header，或改用 Firefox。
   - 无前缀：老格式，整个值直接 DPAPI 解密。
6. **Firefox**：`%APPDATA%\Mozilla\Firefox\Profiles\*\cookies.sqlite`，表 `moz_cookies`，**明文**，同样需先复制。
7. 拼成 `Cookie: name1=value1; name2=value2`。Win-CodexBar 会把验证通过的 Cookie header 缓存（`CookieHeaderCache`），失败（401/403）时清缓存再重新导入。

> **Gotcha**：Chrome / Edge 新版本 ABE 让「自动读浏览器 Cookie」在 Windows 上基本不可用（Edge 是否默认启用 ABE：未核实）。Tokenbar 应把「手动粘贴 Cookie」作为一等公民的设置项，自动读取只作为尽力而为。

### 0.3 其它共用要点

- **JWT 解码**：只做 base64url 解码 payload 取 `exp` / `sub` / `email` / `hd`，**不验签**。
- **凭据存储**：Win-CodexBar 用 Windows Credential Manager（`keyring` crate）保存用户手填的 API key，target 名如 `codexbar-openrouter`、`codexbar-xai`。C# 可用 `CredentialManagement` / P/Invoke `CredWrite`/`CredRead`。
- **读 SQLite 不要产生副作用**：WAL 模式数据库只读打开时，Win-CodexBar 用 `mode=ro`，必要时 `immutable=1`，避免生成 `-wal`/`-shm` 文件。
- **子进程**：Windows 上启动 CLI 要加 `CREATE_NO_WINDOW (0x08000000)`，避免闪黑框。

---

## 1. Codex（OpenAI Codex CLI）

### 1.1 认证来源

| 项 | 值 |
|---|---|
| 文件 | `%CODEX_HOME%\auth.json`，未设置时 `%USERPROFILE%\.codex\auth.json` |
| 字段 | `OPENAI_API_KEY`（API key 模式，**没有**订阅额度）；`tokens.access_token`、`tokens.refresh_token`、`tokens.account_id`、`tokens.id_token`；顶层 `last_refresh`（RFC3339） |
| PAT | `personal_access_token` 或 `personalAccessToken`（可选） |
| 配置 | `config.toml` 中 `chatgpt_base_url`（只允许 HTTPS，localhost 例外；host 是 chatgpt.com 时自动补 `/backend-api`）；`model_provider` 不是 openai 时视为自定义后端，**没有**额度数据 |

- 主 provider **只读不写**：发现 JWT `exp` 距今 < 5 分钟就判定需要重新登录（交给 Codex CLI 自己刷新）。
- 读 `auth.json` 时 CLI 可能正在写入，解析失败要重试（最多 2 次，间隔 50 ms）。

**OAuth 刷新**（仅多账号管理器使用，主路径不刷新）：

```
POST https://auth.openai.com/oauth/token
Content-Type: application/json
{"client_id":"app_EMoamEEZ73f0CkXaXp7hrann","grant_type":"refresh_token",
 "refresh_token":"<rt>","scope":"openid profile email"}
```
- 触发条件：`last_refresh` 超过 8 天，或用量接口返回 401。
- 401 错误码 `refresh_token_reused` / `refresh_token_invalidated` → 终态，需要重新登录。
- Tokenbar 建议：**不要**自己刷新并写回 `~/.codex/auth.json`，以免和 Codex CLI 争抢 refresh token（refresh token 轮换后旧的会失效）。

### 1.2 用量数据源

```
GET https://chatgpt.com/backend-api/wham/usage
Authorization: Bearer <tokens.access_token>
ChatGPT-Account-Id: <tokens.account_id>
User-Agent: CodexBar           （多账号路径用 codex-cli）
Accept: application/json
```

响应字段：

| 字段 | 说明 |
|---|---|
| `plan_type` | 套餐（plus / pro / team …） |
| `rate_limit.primary_window` / `secondary_window` / `code_review_window` | 每个为 `{used_percent, reset_at (unix 秒), limit_window_seconds}` |
| `rate_limit.allowed`、`rate_limit.limit_reached` | 布尔 |
| `rate_limits[]` | 另一种数组形态，结构同上（需兼容） |
| `additional_rate_limits[]` | `{metered_feature, limit_name, rate_limit{primary_window, secondary_window}}`；`codex_spark` 显示为 Spark 5h / 周窗口 |
| `credits` | `{has_credits, unlimited, balance}` |
| `individual_limit` / `individualLimit` | `{limit, used, remaining_percent, resets_at}`（月度 credits 额度） |

附加接口：
- `GET {base}/wham/rate-limit-reset-credits` → `{available_count, credits[{id, reset_type, status, expires_at}]}`（额度重置券，缓存 10 分钟）。
- `GET {base}/subscriptions`：可选，取订阅信息。

PAT 路径（取账号身份）：
```
GET https://auth.openai.com/api/accounts/v1/user-auth-credential/whoami
originator: codex_cli_rs
User-Agent: codex_cli_rs/<ver> (Windows; x86_64)
→ {chatgpt_account_id, chatgpt_plan_type, email}
```

**本地成本日志**（可选，算 token / 费用）：`%USERPROFILE%\.codex\sessions\**\rollout-*.jsonl`；`type:"turn_context"` 行给出 model，`event_msg` 中 `token_count` 事件的 `payload.info.total_token_usage` / `last_token_usage` 给出 token 数。

### 1.3 显示的指标

- Primary：会话窗口（`limit_window_seconds` = 18000，即 5h），`used_percent` %，`reset_at` 倒计时
- Secondary：周窗口（604800 秒）
- 可选：Code review 窗口、Spark 5h/周、月度 credits（`individual_limit`）、credits 余额、重置券数量

### 1.4 Gotchas

- 按 `limit_window_seconds` 判定窗口角色，不要按 primary/secondary 的位置。
- 周窗口用量突然大幅下降可能是服务端提前重置，Win-CodexBar 会做二次确认后再接受新的 reset 时间。
- `OPENAI_API_KEY` 模式与自定义 `model_provider` 都没有订阅额度，应显示「不适用」而不是报错。

---

## 2. Claude Code

### 2.1 认证来源（按顺序）

1. 环境变量 `CODEXBAR_CLAUDE_OAUTH_TOKEN`（及 `CODEXBAR_CLAUDE_OAUTH_SCOPES`）——Tokenbar 可改为自己的变量名。
2. 文件 `%CLAUDE_CONFIG_DIR%\.credentials.json`，默认 `%USERPROFILE%\.claude\.credentials.json`：
   ```json
   {"claudeAiOauth":{"accessToken":"...","refreshToken":"...","expiresAt":1760000000000,
     "scopes":["user:inference","user:profile"],"rateLimitTier":"...","subscriptionType":"..."}}
   ```
   `expiresAt` 为毫秒。（`subscriptionType` 字段：未核实。）
3. Windows Credential Manager：service `Claude Code-credentials`，account = `%USERNAME%`（内容 JSON 结构同上；Windows 上 Claude Code 实际是否写入凭据管理器：未核实，现实中多为文件）。

Win-CodexBar 读取 Claude Code 凭据前需要用户在设置中同意。

**刷新**：
```
POST https://platform.claude.com/v1/oauth/token
Content-Type: application/json
anthropic-beta: oauth-2025-04-20
{"grant_type":"refresh_token","refresh_token":"<rt>",
 "client_id":"9d1c250a-e61b-44d9-88ed-5944d1962f5e","scope":"<原 scopes 空格拼接>"}
→ {access_token, refresh_token, expires_in (默认 3600), scope}
```
- 到期前 5 分钟内才刷新。
- 写回时**只改** `claudeAiOauth` 下的字段，其它键原样保留，使用临时文件 + 原子替换。
- 400/401 且 `invalid_grant` → 终态（需要用户重新 `claude login`）；其它失败退避 5 分钟。

### 2.2 用量数据源

**OAuth（推荐）**：
```
GET https://api.anthropic.com/api/oauth/usage
Authorization: Bearer <accessToken>
anthropic-beta: oauth-2025-04-20
Accept: application/json
```
需要 `user:profile` scope，否则返回 403。

响应字段：

| 字段 | 说明 |
|---|---|
| `five_hour` | `{utilization, resets_at (ISO8601)}`，`utilization` **已经是百分数**（1.0 = 1%） |
| `seven_day` | 周窗口（全部模型） |
| `seven_day_sonnet`、`seven_day_opus` | 按模型的周窗口 |
| `seven_day_design` / `seven_day_oauth_apps`、`seven_day_routines` / `seven_day_omelette` | 其他分项周窗口（代号字段，可能为 null） |
| `extra_usage` | `{is_enabled, used_credits, monthly_limit, currency}`（超额付费） |
| `limits[]` | 新格式：`{kind: session \| weekly_all \| weekly_scoped, group, percent, resets_at, scope.model{id, display_name}}`；**优先使用**，没有时回退到上面的旧字段 |

**Web（Cookie）**：
- 域名：claude.ai、claude.com、console.anthropic.com、anthropic.com；Cookie `sessionKey`（或环境变量 `CLAUDE_AI_SESSION_KEY` / `CLAUDE_WEB_SESSION_KEY`）。
- 请求头：`Origin`/`Referer: https://claude.ai/settings/usage`、Chrome UA、`anthropic-client-platform: web_claude_ai`。
- 组织 ID：Cookie `lastActiveOrg` → 或 `GET https://claude.ai/api/account` 的 `memberships[].organization.uuid` → 或 `GET https://claude.ai/api/organizations`。
- `GET https://claude.ai/api/organizations/{org}/usage?cedar_ember=1`（结构与 OAuth usage 相同）
- `GET .../overage_spend_limit` → `{monthly_credit_limit, used_credits (美分), currency, is_enabled}`
- `GET .../prepaid/credits`（预付余额，字段：未核实）

**其他**：
- Admin API（组织管理员 key）：`https://api.anthropic.com/v1/organizations/cost_report`、`/v1/organizations/usage_report/messages`。
- CLI 兜底：在 PTY 中运行 `claude` 并发送 `/usage`，解析屏幕文本（脆弱，C# 不建议首期做）。
- Win-CodexBar Auto 顺序：Admin API → Web → OAuth → CLI。Tokenbar 建议首期只做 OAuth。

**本地日志**（算 token / 费用）：`%USERPROFILE%\.claude\projects\**\*.jsonl`，以及 `%USERPROFILE%\.config\claude\projects`、`%CLAUDE_CONFIG_DIR%\projects`；`claude_roots.rs` 还扫描 `~/.claude-swap-backup/sessions/<N>-<label>/projects`。每行 assistant 消息带 `message.model` 与 `message.usage{input_tokens, output_tokens, cache_creation_input_tokens, cache_read_input_tokens}`（字段名属 Claude Code 日志通用格式，本次未逐行核对源码：未核实）。

### 2.3 显示的指标

- Primary：Session（5 小时），`utilization` %，`resets_at`
- Secondary：Weekly（7 天，全部模型）
- Tertiary / extra：Sonnet 周、Opus 周、其他 scoped 周窗口
- 成本：Extra usage `used_credits / monthly_limit`（currency）

### 2.4 Gotchas

- **429 很容易触发**：至少退避 5 分钟，指数增长到 1 小时上限；`Retry-After` 为 0 或 1 时不要信。刷新频率建议 ≥ 5 分钟。
- `utilization` 不是 0–1 小数。
- Web 路径会遇到 Cloudflare 质询（返回 HTML），应识别后提示用户。
- 刷新 token 写回文件会和正在运行的 Claude Code 竞争，务必原子写并只改必要字段。

---

## 3. Cursor

### 3.1 认证来源

**首选：Cursor 桌面端本地数据库**（Windows 上最可靠，不受浏览器 ABE 影响）
- 文件：`%APPDATA%\Cursor\User\globalStorage\state.vscdb`（SQLite）
- 只读打开；如果没有 `-wal`/`-shm` 旁车文件导致失败，用 `immutable=1` 重试。
- SQL：`SELECT value FROM ItemTable WHERE key = 'cursorAuth/accessToken'`
- `value` 可能是 UTF-16LE 编码的 blob，需要按编码解码。
- 构造 Cookie：解码 JWT 取 `sub`（形如 `auth0|user_xxx`，取 `|` 后部分还是整个 `sub`：源码使用 `jwt.sub` 整体；具体是否需要截取 `|` 之后：未核实），拼成
  `Cookie: WorkosCursorSessionToken={sub}%3A%3A{accessToken}`
- 每 15 分钟重新读一次（token 会被 Cursor 刷新）。

**备选**：浏览器 Cookie（域名 `cursor.com`、`cursor.sh`，Cookie 名 `WorkosCursorSessionToken`），见 §0.2。

### 3.2 用量数据源（全部带上面的 Cookie）

1. `GET https://cursor.com/api/usage-summary`（camelCase）
   - `billingCycleStart`、`billingCycleEnd`、`membershipType`、`limitType`、`isUnlimited`
   - `individualUsage.plan{used, limit, remaining（美分）, breakdown{included, bonus, total}, autoPercentUsed, apiPercentUsed, totalPercentUsed}`
   - `individualUsage.onDemand{enabled, used, limit, remaining}`、`individualUsage.overall`
   - `teamUsage{onDemand, pooled}`
2. `GET https://cursor.com/api/auth/me` → `email` 等身份信息
3. `POST https://cursor.com/api/dashboard/get-sand-usage-status`，body `{}`，需 `Origin: https://cursor.com`
   → `usagePercent`、`currentPeriodStart`、`nextResetTimestampUtc`、`includedLimitZero`、`hasNonZeroIncludedLimit`、`sandTrialExpiresAt`（Win-CodexBar 显示为 “Grok Bot” 窗口）
4. `POST https://cursor.com/api/dashboard/get-filtered-usage-events`（可选，明细/成本）
   - body：`{"page":1,"pageSize":200,"startDate":"<ms 字符串>","endDate":"<ms 字符串>"}`，必须带 `Origin` 头（CSRF）
   - 最多翻 5 页；响应 `totalUsageEventsCount`、`usageEventsDisplay[{timestamp, model, tokenUsage{inputTokens, outputTokens, cacheWriteTokens, cacheReadTokens, totalCents}, chargedCents}]`
   - 403 后冷却 6 小时
5. 团队预算：`POST https://cursor.com/api/dashboard/teams` 等 dashboard 接口（细节见 `team_budget.rs`，本文未展开）

### 3.3 显示的指标

- Primary：计划用量 `totalPercentUsed`（没有时用 `used/limit`），重置时间 = `billingCycleEnd`
- Secondary：Auto 用量 `autoPercentUsed`
- 模型专项：API 用量 `apiPercentUsed`
- 成本：On-demand `used / limit`（美分 ÷ 100 → USD）
- 套餐：`membershipType`

### 3.4 Gotchas

- 金额单位是**美分**。
- dashboard 的 POST 接口不带 `Origin` 会被 CSRF 拒绝。
- `isUnlimited` 为 true 时没有百分比，显示为信息行。
- 旧版 Cursor 计划按请求次数计费（`limitType` 区分），字段可能不同：未核实。

---

## 4. Gemini CLI

### 4.1 认证来源

- 文件：`%USERPROFILE%\.gemini\oauth_creds.json` → `{access_token, id_token, refresh_token, expiry_date（毫秒）, token_type, scope}`
- CodexBar 还会读 `~/.gemini/settings.json` 的认证类型：`oauth-personal` 支持；`api-key`、`vertex-ai` 不支持（直接报错）。
- 身份：解码 `id_token` 取 `email`、`hd`（Workspace 域）。

**刷新**（access token 过期时）：
```
POST https://oauth2.googleapis.com/token
Content-Type: application/x-www-form-urlencoded
client_id=...&client_secret=...&refresh_token=...&grant_type=refresh_token
```
`client_id` / `client_secret` 查找顺序（Win-CodexBar）：
1. `%USERPROFILE%\.gemini\client_config.json`
2. 从 Gemini CLI 安装包的 JS 里用正则提取：`OAUTH_CLIENT_ID\s*=\s*['"](.*?)['"]`、`OAUTH_CLIENT_SECRET\s*=\s*['"](.*?)['"]`
   - `%APPDATA%\npm\node_modules\@google\gemini-cli\bundle\*.js`
   - `...\node_modules\@google\gemini-cli-core\dist\src\code_assist\oauth2.js`
   - fnm：`%LOCALAPPDATA%\fnm\node-versions\*\installation\node_modules\...`
3. 环境变量 `GEMINI_CLIENT_ID` / `GEMINI_CLIENT_SECRET`（CodexBar 用 `GEMINI_OAUTH_CLIENT_ID` / `GEMINI_OAUTH_CLIENT_SECRET`，还支持 `GEMINI_OAUTH2_JS_PATH`）

刷新后写回 `oauth_creds.json`（更新 `access_token`、`expiry_date`）。

### 4.2 用量数据源

1. 层级/项目：
   ```
   POST https://cloudcode-pa.googleapis.com/v1internal:loadCodeAssist
   Authorization: Bearer <access_token>
   Content-Type: application/json
   {"metadata":{"ideType":"GEMINI_CLI","pluginType":"GEMINI"}}
   ```
   → `currentTier.id`（`free-tier` / `legacy-tier` / `standard-tier`）、`paidTier.name`、`cloudaicompanionProject`、`ineligibleTiers[{reasonCode}]`
2. 额度：
   ```
   POST https://cloudcode-pa.googleapis.com/v1internal:retrieveUserQuota
   Authorization: Bearer <access_token>
   {"project":"<cloudaicompanionProject>"}     （未知时 {}）
   ```
   → `buckets[{remainingFraction, resetTime (ISO8601), modelId, tokenType}]`
   - Win-CodexBar 发送 `{}`；CodexBar（[docs/gemini.md](https://github.com/steipete/CodexBar/blob/HEAD/docs/gemini.md)）发送 `project`，项目 ID 取自 `loadCodeAssist.cloudaicompanionProject`，兜底 `GET https://cloudresourcemanager.googleapis.com/v1/projects` 选 `gen-lang-client*` 或带 `generative-language` 标签的项目。**建议 C# 按 CodexBar 方式带上 project。**

### 4.3 显示的指标

- 每个 `modelId` 取 `remainingFraction` 最小的 bucket；`used% = (1 − remainingFraction) × 100`
- Primary：Pro 系列模型；Secondary：Flash；Tertiary：Flash Lite（CodexBar）
- 窗口：按日（24h）重置，以 `resetTime` 为准
- 套餐：`paidTier.name` 优先；`standard-tier` → Paid；`free-tier` + `hd` → Workspace；`free-tier` → Free；`legacy-tier` → Legacy

### 4.4 Gotchas

- **2026-06-18 起 Google 停止为个人 / AI Pro / Ultra 账号提供 Gemini CLI OAuth**：`loadCodeAssist` 返回 200 但没有 `currentTier`，`ineligibleTiers[].reasonCode == "UNSUPPORTED_CLIENT"`；随后 `retrieveUserQuota` 返回 403 `SUBSCRIPTION_REQUIRED`。应提示用户改用 Antigravity。Workspace / 教育 / Standard / Enterprise 仍可用。
- client secret 依赖本机已安装 Gemini CLI；没装就无法刷新（但未过期的 access token 仍可直接用）。
- 这是 Google 内部接口（`v1internal`），随时可能变化。

---

## 5. Antigravity

Antigravity（Google 的 AI IDE）在本地运行一个 language server，暴露 Connect-RPC（JSON over HTTPS）接口；Win-CodexBar 通过本地 RPC 读额度，不走云端。

### 5.1 认证来源（本地进程发现）

1. 找进程：PowerShell `Get-CimInstance Win32_Process`（C# 可用 WMI `System.Management` 查 `Win32_Process.CommandLine`），匹配进程名 `language_server_windows*`、`language_server.exe`，或 Antigravity CLI `agy.exe`。
2. 从命令行解析参数：`--csrf_token`、`--extension_server_csrf_token`、`--extension_server_port`、`--https_server_port`。`agy` CLI 不需要 CSRF token。
3. 找端口（依次）：
   - 该 PID 正在监听的 TCP 端口（IP Helper API `GetExtendedTcpTable`）
   - `extension_server_port + 0..19`
   - 固定候选 `53835–53838`、`53845`、`53849`
   - 探测：`POST https://127.0.0.1:{port}/exa.language_server_pb.LanguageServerService/GetUnleashData`，body `{}`；返回 200 或 401 即认为端口正确。

### 5.2 用量数据源

公共请求头：
```
Content-Type: application/json
Connect-Protocol-Version: 1
X-Codeium-Csrf-Token: <csrf_token>     （仅 IDE language server）
```
- 自签名证书：必须接受无效证书（C#：`HttpClientHandler.ServerCertificateCustomValidationCallback`，**只对 127.0.0.1 放行**）。
- 绕过系统代理（`UseProxy = false`）。

**新接口（优先）**：
```
POST https://127.0.0.1:{port}/exa.language_server_pb.LanguageServerService/RetrieveUserQuotaSummary
{"forceRefresh":true}
```
响应顶层可能是 `response` / `summary` / `groups` 之一包裹 →
`groups[{displayName, name, buckets[{bucketId, id, displayName, name, description, disabled, remainingFraction, remaining{remainingFraction, case, value}, resetTime（RFC3339 或 epoch 秒）, window}]}]`
- bucket 类型：看 `window` 字段，或按名称别名识别 `session` / `5h` / `5-hour` → 会话窗，`weekly` → 周窗。
- 分组排序：Gemini 优先，其次 Claude / GPT，再其他。

**旧接口（兜底）**：
```
POST .../exa.language_server_pb.LanguageServerService/GetUserStatus
{"metadata":{"ideName":"antigravity","extensionName":"antigravity","ideVersion":"unknown","locale":"en"}}
```
→ `userStatus{email, planStatus.planInfo{planName, planDisplayName}, userTier{name, description}, cascadeModelConfigData.clientModelConfigs[{label, modelId, quotaInfo{remainingFraction, resetTime}}]}`
- 按模型家族归并：Claude（primary）、Gemini Pro（secondary）、Gemini Flash。

**CLI 兜底**：
```
agy -p /usage --output-format json --print-timeout 90s
→ {"status":"SUCCESS","command":{"name":"usage","data":{"groups":[...]}}}
```
- `agy` 路径：环境变量 `ANTIGRAVITY_CLI_PATH` → PATH → `%LOCALAPPDATA%\agy\bin\agy.exe` → `%USERPROFILE%\.local\bin\agy.exe`
- Win-CodexBar 还能在 PTY 中启动受管 `agy`（25 秒超时）；最后兜底只显示离线对话数。

### 5.3 显示的指标

- Primary：最紧张的 5 小时窗口（`remainingFraction` 最小），`used% = (1 − remainingFraction) × 100`
- Secondary：最紧张的周窗口
- 各模型分组作为额外窗口
- 套餐：`planDisplayName` / `userTier.name`；身份：`email`

### 5.4 Gotchas

- 只有 Antigravity IDE 正在运行（或安装了 `agy`）时才有数据。
- 端口每次启动都会变；CSRF token 也会变，必须每次重新发现（可缓存到进程退出）。
- 读其他进程命令行可能需要同等权限；管理员运行的进程从普通进程读不到命令行（未核实具体表现）。
- 接口是内部 protobuf 服务的 JSON 映射，字段名随版本可能改动，解析要宽松（多种包裹层级、`remainingFraction` 可能在 `remaining` 子对象里）。

---

## 6. OpenCode Zen（opencode.ai 按量付费 / 订阅工作区）

### 6.1 认证来源

- 只有浏览器 Cookie（域名 `opencode.ai`），见 §0.2；或手动粘贴 Cookie header。
- 识别未登录：响应正文包含 `login`、`sign in` 或 `auth/authorize`。

### 6.2 用量数据源（SolidStart server function）

```
GET https://opencode.ai/_server?id=<hash>&args=<URL 编码后的 JSON 数组>
X-Server-Id: <hash>
X-Server-Instance: server-fn:<随机 UUID>
User-Agent: Mozilla/5.0 ...
Origin: https://opencode.ai
Referer: https://opencode.ai/workspace/<wrk_id>
Accept: text/javascript, application/json;q=0.9, */*;q=0.8
Cookie: <opencode.ai cookies>
```

| 用途 | server id | args | 解析 |
|---|---|---|---|
| 工作区列表 | `def39973159c7f0483d8793a822b8dbb10d067e12c65455fcb4608459ba0234f` | `[]`（未核实） | 正则 `id\s*:\s*"(wrk_[^"]+)"` 提取工作区 ID |
| 订阅用量 | `7abeebee372f304e050aaaf92be863f4a86490e382f8c79db68fd94040d691b4` | `["wrk_..."]` | `rollingUsage{usagePercent, resetInSec}`、`weeklyUsage{usagePercent, resetInSec}`、`renewAt`；先当 JSON 解析，失败再用正则 |
| 账单（按量付费） | `c83b78a614689c38ebee981f9b39a8b377716db85c1fd7dbab604adc02d3313d` | `["wrk_..."]` | `customerID`、`monthlyUsage`（÷1e8 → USD）、`monthlyLimit`（USD）、`balance`（÷1e8 → USD）、`subscription` |

> 响应是 SolidStart 的序列化格式（`text/javascript`，类似 seroval），不一定是标准 JSON，所以 Win-CodexBar 用「JSON 优先、正则兜底」。

### 6.3 显示的指标

- Primary：滚动窗口（rolling，5 小时）`usagePercent` %，`resetInSec`
- Secondary：周窗口 `weeklyUsage.usagePercent`
- 成本：`monthlyUsage / monthlyLimit`（USD）、余额 `balance`

### 6.4 Gotchas

- **server function 的 hash 是构建产物**，OpenCode 每次重新部署前端都可能改变（推断，未核实）。C# 实现应把 hash 放配置里，可远程更新，或从页面 JS 动态提取（未核实可行性）。
- 金额字段混用 1e8 缩放整数与 USD 浮点，注意区分。
- 只依赖 Cookie，Windows 上受 Chrome ABE 影响大。

---

## 7. OpenCode Go（OpenCode 订阅）

### 7.1 认证来源

1. **本地**（Auto 首选，无需网络）：
   - 候选目录（按顺序）：`%USERPROFILE%\.local\share\opencode\`、`%LOCALAPPDATA%\opencode\`
   - `auth.json`：`{"opencode-go":{"key":"<api key>"}}`（判断是否已登录 OpenCode Go；该 `key` 也可用作下面 API 的 Bearer：推断，未核实 Win-CodexBar 是否这样复用）
   - `opencode.db`：SQLite，只读打开，`busy_timeout` 250 ms
2. **API key**：环境变量 / 设置中的 `OPENCODE_API_KEY`（变量名出自源码常量；是否还读 `auth.json` 未核实）
3. **Console Cookie**：`__Host-console_session`（新 console）；旧版 `auth` / `__Host-auth`

### 7.2 用量数据源

**本地 SQLite**（有 `part` 表时用第二条，否则用第一条）：
```sql
-- 仅 message 表
SELECT CAST(COALESCE(json_extract(data,'$.time.created'), time_created) AS INTEGER) AS createdMs,
       CAST(json_extract(data,'$.cost') AS REAL) AS cost,
       COALESCE(json_extract(data,'$.modelID'),'') AS modelID,
       CASE WHEN json_type(data,'$.tokens')='object' THEN json_extract(data,'$.tokens') END AS tokens
FROM message
WHERE json_valid(data)
  AND json_extract(data,'$.providerID')='opencode-go'
  AND json_extract(data,'$.role')='assistant'
  AND json_type(data,'$.cost') IN ('integer','real');
```
有 `part` 表时：取 `part.data.type = 'step-finish'` 且有 `cost` 的行（通过 `part.message_id` 关联上面的 assistant 消息），再 `UNION ALL` 没有 step-finish 成本的消息本身的 cost（完整 SQL 见 `local.rs` 的 `MESSAGE_AND_PART_USAGE_SQL`）。
- `tokens` JSON：`{total, input, output, reasoning, cache{read, write}}`
- 计算（`cost` 单位 USD）：
  - 会话：过去 5 小时 cost 之和 ÷ **$12**；重置 = 窗口内最早一条 + 5h
  - 周：本 UTC ISO 周（周一 00:00 UTC 起）cost 之和 ÷ **$30**
  - 月：以最早一条记录的日期为锚点的订阅月 cost 之和 ÷ **$60**（无记录时用自然月）
  - 百分比四舍五入到 0.1，夹在 0–100
- 这是**本地估算**，Win-CodexBar 标注为 non-authoritative。

**API key**：
```
GET https://opencode.ai/zen/go/v1/usage
Authorization: Bearer <OPENCODE_API_KEY>
User-Agent: CodexBar
→ {"usage":{"rolling":{...},"weekly":{...},"monthly":{...}}}
   每个窗口：{percent（0–100，不要当成 0–1）, resetInSec 或 resetsAt}
```

**Console Cookie**：
1. `GET https://opencode.ai/console/api/orgs` → `[{id}]`（前缀 `wrk_` 或 `org_`）
2. `GET https://opencode.ai/console/api/go/status`，头 `x-org-id: <id>`
   → `access{endsAt, meters{fiveHour, week, month{usedMicroCents, limitMicroCents, resetsAt}}}`；`access` 为 null = 没有订阅
3. `GET https://opencode.ai/console/api/billing/status` → `{billingMode:"prepaid", mode:"pay-as-you-go", balanceMicroCents（字符串，÷1e8 → USD）}`

**旧版 Web**：`_server` server function（与 Zen 共用 workspaces / billing hash），并抓取 `https://opencode.ai/workspace/{wrk_id}/go` 页面用正则提取百分比与重置时间（`legacy.rs`）。

Win-CodexBar Auto 顺序：本地 SQLite → API key → Web（console → legacy）。

### 7.3 显示的指标

- Primary：5 小时滚动窗口 %
- Secondary：周窗口 %
- Tertiary：月窗口 %
- 成本：本地 token / cost 汇总；console 的余额（USD）

### 7.4 Gotchas

- `usedMicroCents` 这一命名有误导：实际按 ÷1e8 换算为 USD（与 `balanceMicroCents` 相同；`usedMicroCents` 的换算系数：未核实，需抓包确认）。
- $12 / $30 / $60 是硬编码的套餐额度，OpenCode 调价后本地估算会失真。
- `percent` 已是 0–100。

---

## 8. Grok（xAI）

xAI 分两个 provider：**Grok**（消费端订阅 SuperGrok / Grok CLI 的 credits）和 **xAI API**（开发者平台预付余额）。

### 8.1 Grok（消费端 / Grok CLI）

#### 认证来源

- 文件：`%GROK_HOME%\auth.json`，默认 `%USERPROFILE%\.grok\auth.json`（`grok login` 生成）
- 结构：一个对象，键为 scope，值为登录条目：
  ```json
  {"https://auth.x.ai::<client_id>":{"key":"<access token>","refresh_token":"...",
     "expires_at":"2026-10-07T12:00:00Z","email":"...","user_id":"...","team_id":"...","auth_mode":"oidc"}}
  ```
  - 键以 `https://auth.x.ai::` 开头或 `auth_mode == "oidc"` → OAuth 类型；其他 → CLI 类型。
  - `expires_at` 为 RFC3339；已过期则视为未登录。
- **刷新**（到期前 60 秒内）：
  ```
  POST https://auth.x.ai/oauth2/token
  Content-Type: application/x-www-form-urlencoded
  grant_type=refresh_token&client_id=<scope 中 :: 后部分>&refresh_token=<rt>
  → {access_token, refresh_token?, expires_in}
  ```
  - 400/401/403 → 需重新登录。
  - 校验新 access token 的 JWT：`sub` 必须等于原账号 id、`iss == "https://auth.x.ai"`，否则拒绝写回。
  - 写回 `key`、`expires_at`、（如有）新的 `refresh_token`，原子替换。
- 备选：浏览器 Cookie（域名 `grok.com`）。

#### 用量数据源

1. **CLI credits 代理（Bearer 登录优先）**：
   ```
   GET https://cli-chat-proxy.grok.com/v1/billing?format=credits
   Authorization: Bearer <key>
   x-xai-token-auth: xai-grok-cli
   Accept: application/json
   User-Agent: CodexBar
   ```
   响应：
   ```json
   {"config":{"creditUsagePercent":23.5,
              "currentPeriod":{"start":"RFC3339","end":"RFC3339"},
              "billingPeriodStart":"...","billingPeriodEnd":"...",
              "onDemandCap":{"val":0},"onDemandUsed":{"val":0},
              "subscriptionTier":"SuperGrok","productUsage":[...]},
    "subscriptionTier":"..."}
   ```
   - `used% = creditUsagePercent`；没有时用 `onDemandUsed.val / onDemandCap.val × 100`
   - 重置 = `currentPeriod.end`（没有则 `billingPeriodEnd`）；窗口长度 = end − start
   - 响应体上限 256 KB；401/403 → 未登录
   - 只有周期没有百分比时，再用 6 秒超时调一次下面的 gRPC-web 接口补百分比。
2. **套餐名（CLI 类型登录）**：`GET https://cli-chat-proxy.grok.com/v1/settings`（同样的头，2 秒超时）→ `subscription_tier_display`
3. **grok.com gRPC-web（Cookie 路径，或 Bearer 的兜底）**：
   ```
   POST https://grok.com/grok_api_v2.GrokBuildBilling/GetGrokCreditsConfig
   Content-Type: application/grpc-web+proto
   x-grpc-web: 1
   x-user-agent: connect-es/2.1.1
   Origin: https://grok.com
   Referer: https://grok.com/?_s=usage
   Authorization: Bearer <key>   或   Cookie: <grok.com cookies>
   Body（7 字节）：00 00 00 00 02 08 00
   ```
   - 响应头 `grpc-status` 非 0 → 失败（16 = 未认证）。
   - 正文是 gRPC-web 帧：`[flags 1B][len 4B 大端][payload]`，flags `0x00` 数据帧，`0x80` trailer 帧，其他值（压缩等）视为不可用。
   - 用手写 protobuf 扫描（无 .proto），关键字段路径：
     - 百分比：路径最后一段为 1 的 **fixed32（float）** 字段，优先 `[1,1]`，值须在 0–100，多个且不一致则视为未知
     - 重置时间：varint `[1,5,1]`（Unix 秒，有效范围 1.7e9–2.1e9），否则取所有未来时间戳最小值
     - 当前周期：`[1,8,1]` 周期类型（1 或 2），起 `[1,8,2,1]`、止 `[1,8,3,1]`
     - 只递归解析已知消息路径：`[1]`、`[1,2..8]`、`[1,12]`、`[1,6,1..3]`、`[1,8,2]`、`[1,8,3]`、`[1,6,3,2]`、`[1,6,3,3]`
     - 有完整当前周期但没有百分比字段 → 视为 0%（“implicit zero”）
     - 周期类型 1、2 分别对应周/月：未核实（Win-CodexBar 按周期时长 4–12 天 = Weekly、20–45 天 = Monthly 判定）
   - 注释说明：grok.com gRPC 接口对 Bearer 登录需要浏览器持有的 Web Key Exchange 密钥对，所以 Bearer 登录优先走 CLI 代理。
4. **重置券（可选）**：`POST https://grok.com/prod_mc_billing.ConsumerUiSvc/GetRemainingResets`，body `00 00 00 00 00`，同样的 gRPC-web 头，2 秒超时；解析为券列表（字段 10 = token id，字段 30 = 过期时间戳，具体嵌套见 `billing/reset_coupons.rs`：未逐字段核实）。

Win-CodexBar Auto 顺序：auth.json 的 OAuth 条目 → CLI 条目 → 手填 token → 手填 Cookie → 浏览器 Cookie。

#### 显示的指标

- Primary：Credits 用量 %（周期按时长标注 Weekly / Monthly），重置 = 周期结束
- 按产品拆分的用量（`productUsage`）作为明细
- 套餐：SuperGrok / SuperGrok Heavy
- 库存：Limit Reset Credits 数量与最近过期时间

#### Gotchas

- grok.com 的 gRPC-web 没有公开 schema，字段路径是逆向结果，最脆弱；**C# 首期建议只做 CLI 代理 JSON 接口**。
- `auth.json` 可能有多个条目，要按类型选；刷新必须校验身份，防止写错账号。

### 8.2 xAI API（开发者平台，可选）

- 认证：**Management API key**（xAI Console → Settings → Management Keys；推理用 API key 不行），环境变量 `XAI_MANAGEMENT_API_KEY`；另需 **team ID**（`XAI_TEAM_ID`，Console URL 中可见）。
- 余额：
  ```
  GET https://management-api.x.ai/v1/billing/teams/{team_id}/prepaid/balance
  Authorization: Bearer <management key>
  → {"total":{"val":"-1000"}}     账本记法：负数美分 = 余额，USD = -val / 100
  ```
- 30 天每日花费：
  ```
  POST https://management-api.x.ai/v1/billing/teams/{team_id}/usage
  {"analyticsRequest":{"timeRange":{"startTime":"YYYY-MM-DD HH:MM:SS","endTime":"...","timezone":"Etc/GMT"},
    "timeUnit":"TIME_UNIT_DAY","values":[{"name":"usd","aggregation":"AGGREGATION_SUM"}],
    "groupBy":[],"filters":[]}}
  → {"timeSeries":[{"dataPoints":[{"timestamp":"ISO8601","values":[usd]}]}],"limitReached":false}
  ```
- 指标：余额（Balance / Deficit）、近 30 天花费、每日花费曲线；**没有**百分比额度。
- 错误：401/403 = key 类型不对；404 = team ID 不匹配；429 = 限流。

---

## 9. OpenRouter

### 9.1 认证来源

- 普通 API key：设置中填写 / Windows 凭据管理器（target `codexbar-openrouter`，user `api_token`）/ 环境变量 `OPENROUTER_API_KEY`
- 可选 Management API key：设置 / `OPENROUTER_MANAGEMENT_API_KEY`（用于 Activity）；如果 `/key` 返回 `is_management_key: true`，主 key 本身也可用于 Activity。

### 9.2 用量数据源（每个请求 4 秒超时，`/credits` 与 `/key` 并发）

```
GET https://openrouter.ai/api/v1/credits
Authorization: Bearer <key>
→ {"data":{"total_credits":20.0,"total_usage":3.5}}

GET https://openrouter.ai/api/v1/key
Authorization: Bearer <key>
→ {"data":{"limit":10.0,"limit_remaining":6.5,"limit_reset":"monthly","usage":3.5,
           "usage_daily":0.2,"usage_weekly":1.1,"usage_monthly":3.5,"is_management_key":false}}

GET https://openrouter.ai/api/v1/activity[?date=YYYY-MM-DD]     （需 Management key）
→ {"data":[{"date":"YYYY-MM-DD","model":"...","model_permaslug":"...","prompt_tokens":0,
            "completion_tokens":0,"reasoning_tokens":0,"requests":0,"usage":0.0,"byok_usage_inference":0.0}]}
```
- Activity 只返回已完成的 UTC 日；Win-CodexBar 同时请求不带日期（历史）和 `date=昨天` 两次，按 (day, model, ...) 去重合并。
- 单位：均为 USD 浮点。

### 9.3 显示的指标

- Primary：Credits `total_usage / total_credits × 100`%，说明行「$X remaining」（余额 = credits − usage）
- Secondary：API key 限额：`used = limit − clamp(limit_remaining, 0, limit)`；没有 `limit_remaining` 时按 `limit_reset`（daily/weekly/monthly）取对应 `usage_*`，再不行用 `usage`；`limit` 为 null/0 时不显示
- 额外窗口：Daily / Weekly / Monthly spend（仅金额，0%）
- 成本：Activity 汇总（tokens = prompt + completion、requests、模型数、每日花费）；没有 Activity 时用 `usage_monthly`（或 `usage`）作为本月花费

### 9.4 Gotchas

- `/credits` 可能对某些 key 返回 401/403，但 `/key` 仍可用 → 两者独立降级，不要因一个失败而整体报错。
- 基础 URL 是 `https://openrouter.ai/api/v1`，不要拼成 `/api/v1/auth/...`（Win-CodexBar 早期 bug）。
- 没有重置时间概念（除 `limit_reset` 的周期名），倒计时需按 UTC 自然日/周/月推算（Win-CodexBar 未显示倒计时）。

---

## 10. C# 实现难度与建议顺序

| Provider | 首选路径 | 难度 | 主要难点 |
|---|---|---|---|
| OpenRouter | API key → `/credits` + `/key` | 易 | 无 |
| Codex | `~/.codex/auth.json` → `wham/usage` | 易 | 不刷新 token；窗口按时长判定 |
| OpenCode Go | 本地 `opencode.db` / API key | 易（本地、API key）/ 中（console） | SQLite JSON 查询；额度为硬编码估算 |
| xAI API | Management key | 易 | 需要用户提供 team ID |
| Claude Code | `.credentials.json` → `oauth/usage` | 中 | token 刷新写回、429 退避 |
| Cursor | `state.vscdb` → Cookie → `usage-summary` | 中 | UTF-16 blob、Cookie 拼接；浏览器路径受 ABE 影响（难） |
| Gemini CLI | `oauth_creds.json` → `retrieveUserQuota` | 中 | 从 CLI JS 提取 client secret；个人账号已停服 |
| Grok | `~/.grok/auth.json` → CLI credits 代理 | 中（代理 JSON）/ 难（gRPC-web） | 多条目选择、刷新身份校验；gRPC-web 逆向 |
| Antigravity | 本地 language server RPC | 难 | 进程/端口/CSRF 发现、自签证书、解析宽松 |
| OpenCode Zen | 浏览器 Cookie → `_server` | 难 | 只有 Cookie（ABE）、server function hash 会变、非标准序列化 |

建议首期：Codex、Claude Code、Cursor（app DB 路径）、OpenRouter、OpenCode Go（本地 + API key）；第二期：Gemini、Grok（CLI 代理）、xAI；第三期：Antigravity、OpenCode Zen、各类浏览器 Cookie 路径。
