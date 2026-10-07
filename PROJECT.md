# Tokenbar

- 项目标识：be632f3a-2c8c-4a66-b617-11ff50993287
- 类别：Development
- 相对路径：Development/Tokenbar
- 交接入口：HANDOFF.md（决策历史在 docs/DECISION_LOG.md）
- 同步方式：git
- 获取来源：https://github.com/enlivatech/Tokenbar（私有），分支 `main`

## 目标与完成标准

AI 编程工具（Codex、Claude Code 等）用量的系统托盘/菜单栏应用：先做 Windows 原生版，再扩展到全平台，各平台用原生设计语言与框架

第一版支持的工具（AZ 2026-10-07）：Codex、Claude Code、Cursor、Gemini、Antigravity、OpenCode（Zen / Go）、Grok、OpenRouter。

面板内容照 CodexBar：每个工具显示各时间窗口（会话 / 每周等）的已用或剩余百分比、重置倒计时，能拿到的再显示费用和额度；托盘图标反映用量。

完成标准：（和 AZ 确认后补上）

## 平台范围

- 第一步只做 Windows 小工具：系统托盘 + Fluent 弹出面板，目标是做出比 Win-CodexBar 好看、像 Win11 原生的版本。
- macOS 上 CodexBar 已经很成熟，暂不重做；以后扩展到其他平台时再定，各平台单独实现原生 UI，共用一份数据契约（各工具用量的读取规则与数据结构）。

| 平台 | 设计语言 | 框架（暂定） |
|---|---|---|
| Windows | Fluent（Win11） | C# + WinUI 3（Windows App SDK）界面 + Win-CodexBar 的 Rust CLI 取数据 |
| macOS | HIG（菜单栏） | 暂不做，用 CodexBar |
| 其他平台 | 各自官方规范 | 各自默认框架，扩展时再定 |

- 每个平台用自己的原生设计语言、原生图标（含托盘/菜单栏图标、应用图标），不做一套界面套所有平台。

## 参考项目

- [steipete/CodexBar](https://github.com/steipete/CodexBar)：macOS 菜单栏原版，MIT。
- [nesszer/Win-CodexBar](https://github.com/nesszer/Win-CodexBar)：CodexBar 的 Windows fork，MIT。
- 两者都是 MIT：可以复用代码，但要保留原作者版权声明和许可证原文。

## 约束与工作入口

- 规则：`AGENTS.md`（`CLAUDE.md`、`GEMINI.md` 指向它）。

## 环境与验证

- 平台：Windows 10 19041+ / Windows 11，x64 与 ARM64；.NET 8 + Windows App SDK 2.5（自包含、免打包）。
- 数据：Win-CodexBar 的 Rust CLI（子模块 `vendor/Win-CodexBar`），需要 Rust 工具链编译。
- 构建、运行、验证命令见 `AGENTS.md`「项目规则」。
