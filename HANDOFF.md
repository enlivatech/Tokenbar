# 项目交接

- 项目标识：be632f3a-2c8c-4a66-b617-11ff50993287
- 更新时间：2026-10-07 20:20 China Standard Time
- 本次工作者：Cursor Agent（Claude Opus 5.5），AZ-Laptop-7（Windows 11）
- 状态：进行中

## 目标与当前进展

做一个好看的 Windows 原生托盘小工具，显示 AI 编程工具的用量；macOS 继续用 CodexBar，不重做。

已完成第一版可运行骨架：

- 架构定了：fork Win-CodexBar（`enlivatech/Win-CodexBar`，子模块 `vendor/Win-CodexBar`），只用它的 Rust CLI 取数据；界面是自己写的 C# + WinUI 3。
- 托盘图标：Win32 `Shell_NotifyIcon` 自己实现，照 CodexBar `IconRenderer.swift` 的双条几何（36px 画布，上粗为会话、下细为每周，按剩余量填充），按托盘 DPI 抗锯齿绘制，跟随任务栏深浅色，数据过期变暗。
- 弹出面板：亚克力背景、圆角、Fluent 控件；顶部 5 列工具标签（品牌色迷你条），下面是各时间窗口进度条、已用百分比、重置倒计时、Pace 行、信息行、额外用量（花费/上限），底部刷新、退出。点托盘图标开关，失焦或 Esc 关闭。
- 每 5 分钟自动刷新全部工具；一次刷新失败时保留上次的数字并显示错误。

## 成果与版本

- 分支 `main`，快照基于 `7f6c543`（之后的提交即本次成果）。
- 子模块 `vendor/Win-CodexBar` 指向 fork 的 `09b4a95`（上游 `nesszer/Win-CodexBar` 同一提交）。

## 已验证与未验证

- 已验证（本机 2026-10-07）：
  - `dotnet build` 通过；`scripts\build-cli.ps1` 等价的 `cargo build --release` 通过（约 6 分钟）。
  - CLI 实测：Codex、Claude、Cursor、Antigravity、OpenCode Go 能读到真实数据；Gemini（未装 Gemini CLI）、OpenCode Zen（需登录）、Grok（没有 Cookie）、OpenRouter（没有 API Key）返回需要配置的错误。
  - `Tokenbar.exe --open` 面板截图 `docs/screenshots/flyout.png`：Codex 真实数据显示正确。
  - 托盘图标 16/20/24/32 px、深浅两色导出检查 `docs/screenshots/tray-icons.png`：形状、填充、过期变暗正确。
- 未验证：
  - 真实点击托盘图标开关面板（Win11 默认把新图标收进「^」溢出区，需要 AZ 拖出来试）；右键目前和左键一样打开面板。
  - 任务栏在顶部/左/右时的面板定位；多显示器、不同 DPI 切换。
  - 其他工具标签的真实数据显示（只截了 Codex）。
  - Release 打包、自启动、ARM64。

## 关键决策与失败尝试

见 `docs/DECISION_LOG.md`。踩坑：HTTPS 子模块克隆完整历史太慢（13 分钟没完成），改 SSH `--depth 1` 后 11 秒完成；这个环境里 `CARGO_TARGET_DIR` 指向临时缓存，所以 `build-cli.ps1` 显式传 `--target-dir`。

## 接手后的第一个动作

让 AZ 运行 `Tokenbar.exe`，把托盘图标从「^」拖到任务栏，实际点开看效果，收集对外观的意见。

## 后续事项与阻塞

- 设置页（Fluent 设置窗口）：选择启用哪些工具、调整顺序、填 OpenRouter API Key、Grok/OpenCode Cookie 等（写入 CLI 的配置，用 `codexbar config` 命令）。
- 托盘图标加上 CodexBar 的各工具装饰（Codex 眼睛、Claude 方块等），以及「合并图标 / 每个工具一个图标」模式。
- 费用区（今天、近 30 天的 token 与花费，CLI 的 `cost` 命令）、Usage Dashboard / Status Page 链接、通知。
- 打包：把 `codexbar.exe` 放进发布目录 `cli\`，做安装包与开机自启动。
- `docs/PROVIDERS_RESEARCH.md` 由后台调研生成（各工具的认证与接口细节），作为修 CLI 时的参考。
