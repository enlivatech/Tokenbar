# 决策与踩坑日志 — Tokenbar

只追加、不改写；新决定推翻旧决定时另起一行写明替代关系。当前断点见根目录 `HANDOFF.md`。

格式：`- YYYY-MM-DD [决策|踩坑] 一句话`

- 2026-10-07 [决策] 建立项目（AZ-WORKFLOW v2.1）：AI 编程工具（Codex、Claude Code 等）用量的系统托盘/菜单栏应用：先做 Windows 原生版，再扩展到全平台，各平台用原生设计语言与框架
- 2026-10-07 [决策] 先做 Windows 版，后扩展全平台；每个平台用自己的设计语言、原生图标和系统默认的语言框架（Windows 暂定 C# + WinUI 3），参考 MIT 许可的 CodexBar 和 Win-CodexBar
- 2026-10-07 [决策] 第一步只做 Windows 小工具；macOS 的 CodexBar 已经成熟，暂不重做；动机是 Win-CodexBar 太丑，要做出 Win11 原生观感
- 2026-10-07 [决策] 第一版支持 Codex、Claude Code、Cursor、Gemini、Antigravity、OpenCode（Zen / Go）、Grok、OpenRouter；面板内容、托盘图标和额度显示都照 macOS CodexBar
- 2026-10-07 [决策] fork Win-CodexBar 到 enlivatech（GitHub 上公开仓库的 fork 只能是公开的），作为子模块，只用它的 Rust CLI 取数据；界面用 C# + WinUI 3 自己写。不直接 fork CodexBar：它是 Swift/AppKit，Windows 上跑不了
- 2026-10-07 [决策] 托盘图标不用第三方库，直接 Win32 Shell_NotifyIcon + 自绘 HICON，好控制像素、跟随任务栏深浅色
- 2026-10-07 [踩坑] HTTPS 子模块克隆完整历史 13 分钟未完成；改用 SSH 浅克隆（--depth 1）11 秒完成
- 2026-10-07 [踩坑] 从命令行后台启动的程序拿不到前台焦点，面板一弹出就因失焦关闭；加 `--open` 开发参数（StayOpen）来验证
- 2026-10-07 [决策] 设置保持精简：每个工具一个开关，取数路线交给 CLI 的 --source auto；只给需要密钥的工具加输入框，哪个工具实际读不到再单独加选项（AZ 认可，觉得 CodexBar 设置太复杂）
- 2026-10-07 [决策] fork 增加 config set-cookie / remove-cookie / remove-api-key / credentials，让设置窗口通过 CLI 管理密钥（DPAPI 加密的文件只由 CLI 写）
- 2026-10-07 [决策] Cursor 条目名照 CodexBar 用 Total / Cursor / Third Party；按 API 价折算的 cost 显示为「API 价值 / 套餐」，不当作实际花费
- 2026-10-07 [踩坑] 面板经常拿不到前台焦点，Deactivated 不触发导致点外面不收起；改为监听 EVENT_SYSTEM_FOREGROUND
- 2026-10-07 [踩坑] WinUI ProgressBar 底轨固定约 1px 加不粗，改为自绘胶囊条
- 2026-10-07 [决策] Cursor 的 API 价值只放在 Cursor 页，不进总览；总览只汇总真实扣费（AZ 要求）
