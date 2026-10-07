# 项目交接

- 项目标识：be632f3a-2c8c-4a66-b617-11ff50993287
- 更新时间：2026-10-07 21:36 China Standard Time
- 本次工作者：Cursor Agent（Claude Opus 5.5），AZ-Laptop-7（Windows 11）
- 状态：进行中

## 目标与当前进展

做一个好看的 Windows 原生托盘小工具，显示 AI 编程工具的用量；macOS 继续用 CodexBar，不重做。

已完成：

- 架构：fork Win-CodexBar（`enlivatech/Win-CodexBar`，子模块 `vendor/Win-CodexBar`），只用它的 Rust CLI 取数据（`--source auto` 默认自动选路）；界面是自己写的 C# + WinUI 3。设置保持精简（AZ 认可）：每个工具一个开关，只有需要密钥的工具才有输入框。
- 托盘图标：Win32 `Shell_NotifyIcon` 自绘，CodexBar 双条几何，几乎铺满托盘格子，跟随任务栏深浅色，过期变暗。
- 弹出面板：亚克力、Fluent 控件；顶部标签（总览 + 各工具，名字下 2px 品牌色迷你条）；用量条是自绘 6px 胶囊（WinUI ProgressBar 底轨只有 1px）；滚动条隐藏（滚轮仍可滚）；底部刷新、设置、关于、退出。
- 总览页：实际花费汇总卡（只有工具返回真实扣费时才出现；Cursor 的 API 价值只在 Cursor 页显示）+ 每个工具一张卡（前三条额度：名称、条、百分比、短格式重置时间），点卡片进入该工具。
- 点外面收起：监听前台窗口变化（`SetWinEventHook(EVENT_SYSTEM_FOREGROUND)`），不再只靠 Deactivated（面板常拿不到焦点）。
- 设置窗口（Mica + NavigationView）：通用（开机自启、刷新间隔、立即刷新）、工具（开关、状态、数据来源说明、API Key / Cookie 保存与移除、Claude 凭据授权）、关于。
- 套餐名简化（`PlanLabel`：Claude `default_claude_ai` → Pro 等）。
- Cursor：条目名照 CodexBar 改成 Total / Cursor / Third Party；`cost` 是按 API 价折算（"Token cost (metered)"），显示为「API 价值 $x / 套餐 $20」，不再写成花费。
- 配色（AZ 指定）：Codex 蓝 `#3B82F6`、Claude 橙 `#E8803A`、Cursor 绿 `#22C55E`、OpenCode Zen / Go 黄 `#EAB308`；只在 `Models/UsageModels.cs` 的 `Providers.All` 定义一次，标签小条和用量条同色（选中标签也一样）。待定：Antigravity 的绿 `#60BA7E` 和 Cursor 接近，要不要换色还没问到答复。
- Grok 图标换成 CodexBar 的官方标志（原 Win-CodexBar 那个是画的 "G"）。
- fork 新增 `config set-cookie / remove-cookie / remove-api-key / credentials --json`（值走 stdin，从不打印），已推到 fork `main`。

## 成果与版本

- 分支 `main`，快照基于 `cfef14c`（配色提交；之后的提交只是交接记录）。
- 仓库 2026-10-07 起公开（MIT，`LICENSE`、`README.md`）。按 AZ-WORKFLOW，公开仓库平时只提交不推送，「交接项目」时再推。
- `.gitmodules` 的子模块地址改成 HTTPS，方便别人克隆；本机 `.git/config` 里仍是 SSH。
- 总览截图改名为 `docs/screenshots/overview.png`（旧的 `flyout-overview.png` 被看图程序占用，已从仓库移除，本地残留可删）。
- 子模块 `vendor/Win-CodexBar` 指向 fork 的 `c02626e`（在上游 `09b4a95` 之上加了上面的 config 命令）。

## 已验证与未验证

- 已验证（本机 2026-10-07）：
  - `dotnet build` 通过；CLI 重新编译通过，`config --help` 里有 4 个新命令。
  - 数据核对：Codex、Claude、Cursor、Antigravity、OpenCode Go 的百分比与重置时间和 CLI 原始 JSON 逐项一致。Claude 会话 0% 且无重置时间是因为没有进行中的会话。
  - 总览截图 `docs/screenshots/flyout-overview.png`：卡片、6px 条、Cursor 新名称、API 价值卡正确。
- 未验证：
  - 「点外面收起」的新实现（需要 AZ 实点）；真实点击托盘图标。
  - 设置窗口没截图检查过；`config credentials --json` 没实跑（自动审查拦了读取密钥存储的命令）；保存 Key / Cookie 的完整流程。
  - Gemini、OpenCode Zen、Grok、OpenRouter 还没配置，没读到真实数据。
  - 任务栏在其他边、多显示器、Release 打包、ARM64。

## 关键决策与失败尝试

见 `docs/DECISION_LOG.md`。踩坑：HTTPS 子模块克隆太慢改 SSH `--depth 1`；`CARGO_TARGET_DIR` 指向临时缓存，`build-cli.ps1` 显式传 `--target-dir`；CLI JSON 不带 Cursor 的条目名（文字版从 provider metadata 取），Tokenbar 自己补。

## 接手后的第一个动作

运行 `Tokenbar.exe --settings` 截图检查设置窗口，并请 AZ 实点面板外面确认能收起。

## 后续事项与阻塞

- 给 OpenRouter 填 Key、Grok / OpenCode Zen 填 Cookie 后验证真实数据。
- 托盘图标加 CodexBar 的各工具装饰，以及「合并图标 / 每个工具一个图标」模式。
- 费用区（CLI 的 `cost` 命令）、Usage Dashboard / Status Page 链接、通知。
- 打包：`codexbar.exe` 放进发布目录 `cli\`，安装包。
- fork 的 config 命令可以考虑给上游 `nesszer/Win-CodexBar` 提 PR。
- `docs/PROVIDERS_RESEARCH.md`：各工具认证与接口细节。要点：Gemini 个人账号的 Gemini CLI OAuth 从 2026-06-18 起被停用，应提示改看 Antigravity；OpenCode Zen 依赖网站内部接口 hash，容易坏；Claude 接口容易 429。
