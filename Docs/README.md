# 人才贸易 (Talent Trade) — 项目文档

本文档是 Phinix 人才贸易 Mod 的维护手册。所有设计决策、文件说明、架构图均在此目录下。

## 文档索引

| 文件 | 内容 |
|------|------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | 整体架构、数据流、线程模型 |
| [FILES.md](FILES.md) | 每个源文件的用途与关键 API |
| [PROTOCOL.md](PROTOCOL.md) | 通信协议格式、消息类型一览 |
| [PHASES.md](PHASES.md) | 开发阶段规划与当前进度 |

## 快速概览

- **Mod 类型**: Phinix 聊天室扩展插件
- **功能**: 玩家间交易/出售/租借殖民者 (Pawn)
- **三种模式**: 直接交易、公共市场、租借
- **通信方式**: 通过外部 HTTP 中继服务器，借助 Phinix 聊天频道传输协议消息
- **目标版本**: RimWorld 1.3 ~ 1.6
- **依赖**: Phinix、Harmony
