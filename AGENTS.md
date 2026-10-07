# AGENTS.md — Tokenbar

AI 编程工具（Codex、Claude Code 等）用量的系统托盘/菜单栏应用：先做 Windows 原生版，再扩展到全平台，各平台用原生设计语言与框架

## 项目交接（AZ-WORKFLOW v2.1）

**每次会话开始，先读 `HANDOFF.md`**（再按需读 `PROJECT.md`），了解当前进展、下一步和阻塞再动手；不论使用者有没有说下面的口令。

**先同步**：会话开始先 `git fetch`；落后远端且工作区干净，就 `git pull --ff-only` 拉到最新再读断点（换电脑接手时，另一台推上来的进展就到了）；有本地改动或分叉时先报告，不自动处理。

**断点落后时自动补齐**：读完 `HANDOFF.md` 后，看它「成果与版本」写的快照基线提交之后的新提交（`git log --oneline <基线>..HEAD`）和 `git status --short`。这些是断点没写到的进展：先简短补进 `HANDOFF.md`，再继续，不要照着旧断点重做。

**随做随记**：每完成一个阶段性成果（一个功能、一次修复、一次验证结果），就更新 `HANDOFF.md`（进展、已验证与未验证、下一步）并提交，提交后立刻 `git push`（例外：公开仓库、没有远端的项目只提交不推送；`.github/workflows` 里有推送即构建、发布或部署的（`on: push` 到当前分支），攒到「交接项目」时一起推。推送失败就说明原因，下一步先补推）。会话中途断掉或直接换电脑，GitHub 上最多落后一步。交接前跑 `~/Projects/Workbench/agent-rules/check-projects.sh --here`（Windows 用 Git Bash 跑同一个脚本）确认没有漏推、漏记。

- 项目身份见 `PROJECT.md`；最新断点只记在根目录 `HANDOFF.md`；决策历史在 `docs/DECISION_LOG.md`（只追加，不改写）。项目管理不使用 Obsidian。
- 口令按 AZ-WORKFLOW v2.1 执行（`~/Projects/Workbench/agent-rules/AZ-WORKFLOW.md`，私有仓库 enlivatech/agent-rules），要点：
  - **继续项目 / 开始项目 / `az-start`**：工作区干净才 `git pull --ff-only`；有本地改动时不 pull、不 stash、不覆盖，先报告；遇到分叉、冲突、认证或网络问题时停止，不得改用 merge、rebase、reset 或 force。拉取后重新读取本文件、`PROJECT.md`、`HANDOFF.md`，核对环境与断点后，从「接手后的第一个动作」继续。
  - **项目进度 / `az-status`**：严格只读，不编辑、不安装依赖、不 pull、不 commit、不 push。核对分支、commit、`git status --short`、远端分支与 `HANDOFF.md`，报告目标、进展、未提交改动、下一步、阻塞和核对时间；无法访问的来源标「未验证」。
  - **交接项目 / `az-handoff`**：审查 diff 和敏感信息（`.env`、密钥、token、密码、个人数据、缓存），按改动风险做相称验证；更新 `HANDOFF.md`（写真实验证结果和快照基于的提交，不写本次提交自己的哈希），在决策日志末尾追加本次关键决定；只暂存相关文件，中文提交，普通 `git push` 并核对远端；不得 force push。推送失败时保留本地断点并说明原因。

## 项目规则

- 运行、构建、验证命令：（建好后补上）
- 扩大范围或重构既有架构前，先和 AZ 确认。
- 不得提交 `.env`、密钥、token、密码或个人数据。
