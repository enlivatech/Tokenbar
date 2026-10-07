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
