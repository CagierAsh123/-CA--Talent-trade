# 人才贸易 (Talent Trade) — 项目总结

> 最后更新: 2026-03-15 | 编译验证: 0 错误 0 警告

RimWorld 1.6 多人 Pawn 交易 Mod，依赖 Phinix（多人聊天 Mod）和 Harmony。
玩家可以通过 HTTP Relay 服务器跨存档交易、出售、租借殖民者。

## 三种交易模式

- **公共市场 (Market)** — 上架 Pawn 标价银币，其他玩家浏览购买，空投舱送达
- **直接交易 (Direct Trade)** — 两人实时协商，各自添加 Pawn/银币，双方锁定后同时交换
- **租借 (Rental)** — 上架 Pawn 设日租金/押金/最大天数，租方到期归还，死亡则从缓存复活

## 目录结构与文件说明

```
【CA】人才贸易-Talent trade/
├── About/
│   └── About.xml                     # Mod 元数据 (ID: Natsuki.TalentTrade)
├── Assemblies/
│   ├── 0Harmony.dll                  # Harmony 运行时
│   └── TalentTrade.dll               # 编译产物
├── Docs/                             # 文档与运维工具
├── Languages/
│   ├── ChineseSimplified/Keyed/      # 中文翻译
│   └── English/Keyed/                # 英文翻译
├── Server/
│   └── relay.py                      # Relay 服务器（基础版，无认证）
└── Source/TalentTrade/               # C# 源码
    └── TalentTrade.csproj            # SDK-style 项目文件 (.NET 4.7.2)
```

## C# 源文件详解

### 入口与配置

| 文件 | 作用 |
|------|------|
| `TalentTradeMod.cs` | Mod 入口，继承 `Mod`，注册 Harmony 补丁，创建设置页 |
| `TalentTradeSettings.cs` | Mod 设置（通知开关等），继承 `ModSettings` |

### Core/ — 核心逻辑

| 文件 | 作用 | 关键细节 |
|------|------|----------|
| `TalentTradeManager.cs` | 中央状态管理器 (~1700行) | 管理所有交易状态、消息路由、主线程队列。`Update()` 每帧调用，处理 incoming 消息并分发到对应 handler |
| `TalentTradeProtocol.cs` | 协议编解码 | 消息格式: `PHXTT\|v1\|<type>\|<fields...>`，管道符分隔。Build* 方法构造消息，TryParse 解析 |
| `TalentTradeTransport.cs` | HTTP 通信层 | 轮询 GET 700ms 间隔，发送 POST 80ms 间隔。ThreadPool 异步。带 API Key + HMAC-SHA256 签名 |
| `PawnSerializer.cs` | Pawn → Base64 | Pawn → Scribe XML → GZip → Base64。用 `ScribeSaver.DebugOutputFor` 导出 |
| `PawnDeserializer.cs` | Base64 → Pawn | Base64 → GZip → XML → Pawn。含安全校验: 大小限制、XXE 防护、结构验证、humanlike 检查 |
| `DefManifestHelper.cs` | Def 兼容性检查 | 交易前交换双方的 Def 列表，检测缺失 mod 导致的不兼容 |

### Models/ — 数据模型

| 文件 | 作用 |
|------|------|
| `DirectTrade.cs` | 直接交易状态 + TradeOffer 模型 |
| `MarketListing.cs` | 市场上架条目 |
| `RentalContract.cs` | 租借合约（含到期时间、押金、缓存 Pawn 数据） |
| `PawnSummary.cs` | 轻量 Pawn 摘要（名字、种族、性别、年龄、技能），用于 UI 展示 |
| `TradeOfferSerializer.cs` | TradeOffer ↔ Base64 序列化 |

### Patches/ — Harmony 补丁

| 文件 | 作用 |
|------|------|
| `RootUpdatePatches.cs` | Hook `Root.Update()` 驱动 `TalentTradeManager.Update()` |
| `ServerTabPatches.cs` | 向 Phinix ServerTab 注入 "Talent Trade" 标签页 |
| `ChatFilterPatches.cs` | 过滤 Phinix 聊天中的 PHXTT 协议消息，不显示给用户 |
| `PawnTextureAtlasGCPatch.cs` | 修复原版 `PawnTextureAtlas.GC()` 的 KeyNotFoundException |

### UI/ — 界面

| 文件 | 作用 |
|------|------|
| `TalentTradeTab.cs` | 顶层标签页，包含 3 个子面板切换 |
| `DirectTradePanel.cs` | 在线用户列表 + 活跃交易面板 |
| `DirectTradeWindow.cs` | 交易协商窗口（双方 offer 分栏视图） |
| `MarketPanel.cs` | 市场浏览/上架/购买面板 |
| `RentalPanel.cs` | 租借浏览/上架/租用/归还面板 |

### GameComponents/

| 文件 | 作用 |
|------|------|
| `TalentTradeGameComponent.cs` | 每存档持久化，管理 Pawn 备份/恢复（加载存档时） |

## 通信架构

```
  玩家A (C# Mod)                 Relay Server (Python)              玩家B (C# Mod)
       │                              │                                  │
       │── POST /v1/raw ─────────────>│                                  │
       │   Headers:                   │                                  │
       │     X-Api-Key: tt-9f3a...    │                                  │
       │     X-Signature: hmac(body)  │                                  │
       │     X-Room: talent-trade     │                                  │
       │     X-Sender: uuid-A        │                                  │
       │   Body: PHXTT|v1|mlist|...   │                                  │
       │                              │<── GET /v1/raw?after_id=N ───────│
       │                              │──> id\tbase64(msg)\n ───────────>│
```

- 轮询间隔: 700ms (GET), 发送间隔: 80ms (POST)
- 消息格式: `PHXTT|v1|<type>|<field1>|<field2>|...`
- 大数据（Pawn）: GZip 压缩 + Base64，超大时用 BlobPart 分片

## 安全机制

| 层级 | 机制 | 位置 |
|------|------|------|
| 传输认证 | API Key (`X-Api-Key` header) | relay.py + Transport.cs |
| 消息完整性 | HMAC-SHA256 签名 (`X-Signature` header) | relay.py + Transport.cs |
| 速率限制 | 60 POST/分钟/IP | relay.py |
| 消息体限制 | 512KB max | relay.py |
| Pawn 大小限制 | 4MB Base64 / 2MB XML | PawnDeserializer.cs |
| XXE 防护 | 禁止 DOCTYPE/ENTITY + XmlResolver=null | PawnDeserializer.cs |
| 结构校验 | 必须含 Pawn Class/def/kindDef，深度≤50，节点≤10000 | PawnDeserializer.cs |
| 数据校验 | humanlike 种族检查，技能等级钳制 (max 20) | PawnDeserializer.cs |

## 线程模型

- **Unity 主线程**: UI 渲染、游戏状态修改、Manager.Update()、Scribe 操作
- **ThreadPool 发送线程**: HTTP POST，失败自动重入队列，退避 1s
- **ThreadPool 轮询线程**: HTTP GET，失败退避 2s
- **共享状态保护**: OutgoingLock / IncomingLock / StateLock 三把独立锁
- **跨线程调度**: `EnqueueMainThread()` 将回调排入主线程队列

## Pawn 序列化流程

```
序列化: Pawn → ScribeSaver.DebugOutputFor → XML string → GZip → Base64
反序列化: Base64 → GZip decompress → XML safety check → structure check
         → XmlDocument.LoadXml → ScribeExtractor.SaveableFromNode<Pawn>
         → CrossRef resolve → PostLoadInit → PostProcess (ID重生成/Ideo修复/Job重置)
```

PostProcess 关键步骤:
- 重新生成 thingID（Pawn/装备/服装/库存/hediff/gene）
- 设置玩家阵营
- 修复意识形态
- 清除无效目标引用
- 重置 Job/Stance/VerbTracker

## 编译

```bash
cd "Source/TalentTrade"
dotnet build TalentTrade.csproj -c Release
```

依赖:
- .NET Framework 4.7.2 (C# 7.3)
- RimWorld DLL: `B:\SteamLibrary\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\`
- Phinix DLL: `B:\rimworld-code\_SourceCode\phinix\` (PhinixClient, Utils, Chat, Trading)
- Harmony: `Assemblies\0Harmony.dll`

## Docs/ 文件说明

| 文件 | 用途 |
|------|------|
| `PROJECT.md` | 本文件，项目总结 |
| `PROTOCOL.md` | 通信协议详细规格 |
| `relay.py` | Relay 服务器源码（带注释可读版） |
| `relay-tester.html` | 浏览器端 Relay 测试客户端 |
| `setup.txt` | 服务器部署脚本（压缩版，可直接粘贴到 SSH） |

## 密钥配置

| 用途 | 值 | 出现位置 |
|------|---|----------|
| API Key | `tt-9f3a7c2e1b4d6058e7a2c9d4f1b3e5a8` | relay.py, Transport.cs, relay-tester.html |
| Signing Key | `tt-sign-b7e2f4a19c3d5068d1e4a7b2c8f0e3d6` | relay.py, Transport.cs, relay-tester.html |
| Relay 地址 | `http://114.55.115.143:8080` | Transport.cs, relay-tester.html |
| Room 名 | `talent-trade` | Transport.cs, relay-tester.html |
