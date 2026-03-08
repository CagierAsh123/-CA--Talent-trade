# 架构设计

## 整体架构

```
┌─────────────────────────────────────────────────────────┐
│                    RimWorld 主循环                        │
│                   Root.Update()                          │
└──────────────┬──────────────────────────────────────────┘
               │ Harmony Postfix
               ▼
┌──────────────────────────────────────────────────────────┐
│              TalentTradeManager.Update()                  │
│                                                          │
│  ┌─────────────────┐  ┌──────────────┐  ┌─────────────┐ │
│  │ Transport.Update │  │ PollRelay    │  │ RunMainThread│ │
│  │ (发送/接收调度)   │  │ Buffer       │  │ Queue       │ │
│  └────────┬────────┘  └──────┬───────┘  └─────────────┘ │
│           │                  │                           │
│           ▼                  ▼                           │
│  ┌─────────────────┐  ┌──────────────────┐              │
│  │ HTTP 中继服务器   │  │ ProcessProtocol  │              │
│  │ (后台线程池)      │  │ Message          │              │
│  └─────────────────┘  │  → TryParse       │              │
│                       │  → Handle*        │              │
│                       │  → 更新状态字典    │              │
│                       └──────────────────┘              │
└──────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                  Phinix ServerTab UI                      │
│                                                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐               │
│  │ 聊天(原有) │  │ 用户列表  │  │ 人才贸易  │ ← Harmony 注入│
│  └──────────┘  └──────────┘  └────┬─────┘               │
│                                   │                      │
│                    ┌──────────────┼──────────────┐       │
│                    ▼              ▼              ▼       │
│              ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│              │ 直接交易   │  │ 公共市场   │  │ 租借     │   │
│              └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────┘
```

## 线程模型

本 Mod 涉及三类线程：

| 线程 | 职责 | 访问方式 |
|------|------|----------|
| Unity 主线程 | UI 绘制、游戏状态修改、Manager.Update() | 直接调用 |
| ThreadPool 发送线程 | HTTP POST 发送协议消息到中继服务器 | TalentTradeTransport.SendOnceWorker() |
| ThreadPool 轮询线程 | HTTP GET 从中继服务器拉取新消息 | TalentTradeTransport.PollWorker() |

### 线程安全策略

所有共享状态均使用独立的 `object` 锁保护：

```
MarketLock      → MarketListings 字典
RentalLock      → RentalContracts 字典
TradeLock       → ActiveTrades 字典
BlobLock        → PendingBlobs 字典
ProcessedLock   → ProcessedProtocolKeys 去重集合
MainThreadQueue → 自身作为锁
```

后台线程 **绝不** 直接修改游戏状态。需要操作 UI 或游戏对象时，通过 `EnqueueMainThread(Action)` 投递到主线程队列，在下一帧的 `RunMainThreadQueue()` 中执行。

## 数据流

### 发送消息

```
用户操作 (如上架出售)
  → TalentTradeProtocol.BuildMarketList(...)   // 构造协议字符串
  → TalentTradeManager.SendProtocol(message)   // 附加本地 UUID
  → TalentTradeTransport.EnqueueProtocol(...)  // 入队
  → SendOnceWorker()                           // 后台线程 HTTP POST
  → 中继服务器 /v1/raw
```

### 接收消息

```
中继服务器 /v1/raw?room=talent-trade&after_id=...
  ← PollWorker()                               // 后台线程 HTTP GET
  ← ParseRawResponse()                         // 解析 id\tbase64data
  ← IncomingQueue.Enqueue(decoded)             // 入队
  ← TalentTradeManager.PollRelayBuffer()       // 主线程每帧最多取 20 条
  ← ProcessProtocolMessage()                   // 解析 + 分发
  ← Handle*(parts)                             // 更新状态字典
  ← UI 在下一帧读取快照并渲染
```

### 大数据传输 (Blob)

完整的 Pawn XML 数据经 GZip 压缩 + Base64 编码后可能超过单条消息限制。此时拆分为多个 `BlobPart`：

```
原始数据 → GZip → Base64 → 按固定长度切片
  → 每片构造 BlobPart 消息: PHXTT|v1|blob|blobId|partIndex|totalParts|b64PartData
  → 接收方 HandleBlobPart() 收集所有片段
  → 全部到齐后拼接还原 → 作为完整协议消息重新处理
```

## 与 Phinix 的集成方式

本 Mod 通过 Harmony 补丁与 Phinix 集成，**不修改** Phinix 源码：

1. **Tab 注入** — 在 `ServerTab` 构造函数中追加一个 TabRecord
2. **内容绘制** — 在 `ServerTab.DoWindowContents` 中检测当前 Tab 并绘制
3. **聊天过滤** — 在 `ChatMessageList` 的多个方法中过滤掉 `PHXTT|` 前缀的消息
4. **按键拦截** — 在非聊天 Tab 时阻止 Enter 键触发聊天发送

## 中继服务器

| 配置项 | 值 |
|--------|-----|
| 地址 | `http://39.96.216.77/rp` |
| 房间名 | `talent-trade` |
| 轮询间隔 | 700ms |
| 发送间隔 | 80ms |
| 请求超时 | 5000ms |
| 每次拉取上限 | 200 条 |
| 首次连接历史 | 最近 20 分钟 |

中继服务器与 Phinix 红包 Mod 共用同一套基础设施，通过不同的房间名隔离。
