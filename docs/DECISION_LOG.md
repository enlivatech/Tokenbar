# 决策与踩坑日志 — Tokenbar

只追加、不改写；新决定推翻旧决定时另起一行写明替代关系。当前断点见根目录 `HANDOFF.md`。

格式：`- YYYY-MM-DD [决策|踩坑] 一句话`

- 2026-10-07 [决策] 建立项目（AZ-WORKFLOW v2.1）：AI 编程工具（Codex、Claude Code 等）用量的系统托盘/菜单栏应用：先做 Windows 原生版，再扩展到全平台，各平台用原生设计语言与框架
- 2026-10-07 [决策] 先做 Windows 版，后扩展全平台；每个平台用自己的设计语言、原生图标和系统默认的语言框架（Windows 暂定 C# + WinUI 3），参考 MIT 许可的 CodexBar 和 Win-CodexBar
- 2026-10-07 [决策] 第一步只做 Windows 小工具；macOS 的 CodexBar 已经成熟，暂不重做；动机是 Win-CodexBar 太丑，要做出 Win11 原生观感
