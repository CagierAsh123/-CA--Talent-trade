# 文件说明

## 目录结构

```
【CA】人才贸易-Talent trade/
├── About/
│   └── About.xml                          # Mod 元数据
├── Assemblies/
│   ├── 0Harmony.dll                       # Harmony 运行时 (复制自红包 Mod)
│   └── TalentTrade.dll                    # 编译产物 (Release 输出)
├── Docs/
│   ├── README.md                          # 文档索引
│   ├── ARCHITECTURE.md                    # 架构设计
│   ├── FILES.md                           # ← 本文件
│   ├── PROTOCOL.md                        # 协议格式
│   └── PHASES.md                          # 开发阶段
├── Languages/
│   ├── ChineseSimplified/Keyed/Keyed.xml  # 简体中文翻译
│   └── English/Keyed/Keyed.xml            # 英文翻译
└── Source/TalentTrade/
    ├── TalentTrade.csproj                 # 项目文件
    ├── TalentTradeMod.cs                  # Mod 入口
    ├── TalentTradeSettings.cs             # 设置
    ├── Core/
    │   ├── TalentTradeManager.cs          # 核心管理器
    │   ├── TalentTradeProtocol.cs         # 协议编解码
    │   └── TalentTradeTransport.cs        # HTTP 中继传输层
    ├── Models/
    │   ├── PawnSummary.cs                 # Pawn 摘要 (用于列表展示)
    │   ├── MarketListing.cs               # 市场挂牌
    │   ├── DirectTrade.cs                 # 直接交易会话
    │   └── RentalContract.cs              # 租借合约
    ├── Patches/
    │   ├── RootUpdatePatches.cs           # 主循环钩子
    │   ├── ServerTabPatches.cs            # Phinix Tab 注入
    │   └── ChatFilterPatches.cs           # 聊天消息过滤
    └── UI/
        └── TalentTradeTab.cs              # 交易 Tab 界面
```

---

## 逐文件说明

### About/About.xml

Mod 的身份声明文件，RimWorld 启动时读取。

- `packageId`: `Natsuki.TalentTrade`
- 支持版本: 1.3 ~ 1.6
- 硬依赖: `Thomotron.Phinix` (Phinix 聊天)、`brrainz.harmony` (Harmony)
- 加载顺序: 在 Phinix 和红包 Mod 之后

---

### Source/TalentTrade/TalentTrade.csproj

MSBuild SDK 风格项目文件。

| 配置 | 值 |
|------|-----|
| 目标框架 | .NET Framework 4.7.2 |
| C# 版本 | 7.3 |
| Release 输出 | `../../Assemblies/` |
| Debug 输出 | `bin/Debug/` |

引用的外部程序集 (全部 `Private=False`，不复制到输出):
- `0Harmony.dll` — Harmony 补丁框架
- `Assembly-CSharp.dll` — RimWorld 核心 (`Verse`、`RimWorld` 命名空间)
- Unity 模块: `CoreModule`、`IMGUIModule`、`TextRenderingModule`
- Phinix: `9-PhinixClient.dll`、`3-Utils.dll`、`7-Chat.dll`、`8-Trading.dll`
- .NET: `System`、`System.Core`、`System.Xml`、`System.IO.Compression`

---

### Source/TalentTrade/TalentTradeMod.cs

**命名空间**: `TalentTrade`
**类**: `TalentTradeMod : Mod`

Mod 入口点。RimWorld 加载 Mod 时实例化此类。

| 方法 | 作用 |
|------|------|
| 构造函数 | 保存单例 `Instance`；加载 Settings；`harmony.PatchAll()` 应用所有补丁；若 Phinix Client 已就绪则立即初始化 Manager |
| `SettingsCategory()` | 返回设置页标签名 (翻译键 `TalentTrade_settingsCategory`) |
| `DoSettingsWindowContents(Rect)` | 绘制"启用交易消息提醒"复选框 |

---

### Source/TalentTrade/TalentTradeSettings.cs

**类**: `TalentTradeSettings : ModSettings`

持久化设置，通过 RimWorld Scribe 系统存档。

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `EnableNotifications` | bool | true | 是否显示交易事件的游戏内提醒 |

---

### Source/TalentTrade/Core/TalentTradeManager.cs

**类**: `TalentTradeManager` (静态类)

整个 Mod 的中枢。管理所有交易状态，处理收到的协议消息，提供线程安全的数据访问。

**状态字典**:

| 字典 | 锁 | 内容 |
|------|-----|------|
| `MarketListings` | `MarketLock` | 公共市场挂牌 |
| `RentalContracts` | `RentalLock` | 租借合约 |
| `ActiveTrades` | `TradeLock` | 直接交易会话 |
| `PendingBlobs` | `BlobLock` | 待拼装的大数据分片 |
| `ProcessedProtocolKeys` | `ProcessedLock` | 已处理消息 ID (去重) |
| `MainThreadQueue` | 自身 | 待在主线程执行的 Action 队列 |

**关键方法**:

| 方法 | 作用 |
|------|------|
| `Initialize(Client)` | 一次性初始化，挂载断线清理回调 |
| `Update()` | 每帧调用：驱动 Transport → 拉取消息 → 执行主线程队列 |
| `SendProtocol(string)` | 发送协议消息 (附加本地 UUID) |
| `GetLocalUuid()` | 获取当前玩家 UUID |
| `GetLocalDisplayName()` | 获取当前玩家显示名 (通过 `TryGetDisplayName`) |
| `Get*Snapshot()` | 线程安全的状态快照，供 UI 读取 |
| `ProcessProtocolMessage(string)` | 解析 → 去重 → 分发到 Handle* |
| `HandleMarketList/Delist` | ✅ 已实现：市场上架/下架 |
| `HandleRentalList/Delist` | ✅ 已实现：租借上架/下架 |
| `HandleBlobPart` | ✅ 已实现：大数据分片拼装 |
| 其余 Handle* | ⬜ 桩函数，待后续阶段实现 |

---

### Source/TalentTrade/Core/TalentTradeProtocol.cs

**枚举**: `TalentTradeMessageType` (25 种消息类型)
**类**: `TalentTradeProtocol` (静态类)

定义所有消息的线格式。每条消息是管道符分隔的字符串：`PHXTT|v1|<类型码>|字段1|字段2|...`

| 方法 | 作用 |
|------|------|
| `IsProtocolMessage(string)` | 判断字符串是否包含 `PHXTT\|` 前缀 |
| `EncodeField(string)` / `DecodeField(string)` | Base64 编解码，用于在管道分隔消息中嵌入任意文本 |
| `Build*(...)` | 20+ 个构造方法，每种消息类型一个 |
| `TryParse(string, out type, out parts)` | 解析任意消息：定位前缀 → 分割 → 校验版本 → 映射类型码 |

详细协议格式见 [PROTOCOL.md](PROTOCOL.md)。

---

### Source/TalentTrade/Core/TalentTradeTransport.cs

**类**: `TalentTradeTransport` (内部静态类)

HTTP 中继传输层。完全独立于 Phinix 自身的网络通信。

| 方法 | 作用 |
|------|------|
| `Update()` | 每帧调用，按频率限制调度后台发送/轮询 |
| `EnqueueProtocol(msg, uuid)` | 消息入队 (附唯一 EventId) |
| `TryDequeueIncoming(out msg)` | 从接收队列取一条消息 |
| `SendOnceWorker()` | 后台线程：出队一条 → POST `/v1/raw` |
| `PollWorker()` | 后台线程：GET `/v1/raw?room=...&after_id=...` |
| `ParseRawResponse(string)` | 解析中继响应 (首行=lastId，后续行=id\tbase64data) |
| `Compress(string)` / `Decompress(string)` | GZip + Base64 工具方法 |

---

### Source/TalentTrade/Models/PawnSummary.cs

**类**: `PawnSummary`

Pawn 的轻量摘要，用于在列表中展示（不是完整 Pawn 数据）。

| 字段 | 说明 |
|------|------|
| `Name` | 名字 |
| `Gender` | 性别 ("Male" / "Female") |
| `BiologicalAge` | 生理年龄 |
| `RaceDefName` | 种族 Def 名 (默认 "Human") |
| `SkillsSummary` | 技能概要文本 |
| `TraitsSummary` | 特性概要文本 |
| `HealthSummary` | 健康概要文本 |

| 方法 | 作用 |
|------|------|
| `ToBase64()` | 序列化为换行分隔文本 → Base64 |
| `FromBase64(string)` | 反序列化，容错处理 |
| `GetDisplayLabel()` | 格式化显示：`"名字 ♂ 25岁"` |

---

### Source/TalentTrade/Models/MarketListing.cs

**枚举**: `MarketListingState` { Active, Sold, Delisted }
**类**: `MarketListing`

公共市场的一条挂牌记录。

| 字段 | 说明 |
|------|------|
| `Id` | 挂牌唯一 ID |
| `SellerUuid` / `SellerName` | 卖家信息 |
| `Summary` | PawnSummary 摘要 |
| `PriceSilver` | 售价 (银子) |
| `State` | 状态 |
| `HeldPawn` | 仅本地：卖家侧持有的已脱离地图的 Pawn 引用 |

---

### Source/TalentTrade/Models/DirectTrade.cs

**枚举**: `DirectTradeState` { Pending, Accepted, Negotiating, Confirmed, Executing, Completed, Cancelled }
**类**: `TradeOffer` — 一方的报价 (Pawn 列表 + 银子 + 物品)
**类**: `DirectTrade` — 一次 1v1 交易会话

| 字段 | 说明 |
|------|------|
| `Id` | 交易唯一 ID |
| `InitiatorUuid/Name` | 发起方 |
| `TargetUuid/Name` | 目标方 |
| `InitiatorOffer` / `TargetOffer` | 双方报价 |
| `InitiatorConfirmed` / `TargetConfirmed` | 双方是否锁定确认 |
| `State` | 交易状态 |
| `HeldPawns` / `HeldItems` | 仅本地：已脱离地图的 Pawn/物品引用 |

---

### Source/TalentTrade/Models/RentalContract.cs

**枚举**: `RentalContractState` { Listed, Active, Returned, PawnLost }
**类**: `RentalContract`

租借合约。

| 字段 | 说明 |
|------|------|
| `Id` | 合约唯一 ID |
| `OwnerUuid/Name` | 出租方 |
| `RenterUuid/Name` | 承租方 |
| `Summary` | PawnSummary 摘要 |
| `PricePerDay` / `Deposit` / `MaxDays` | 日租金、押金、最长天数 |
| `RentedDays` / `StartTick` / `ExpiryTick` | 运行时状态 |
| `RentedPawnThingID` | 仅本地：租出 Pawn 的 ThingID |
| `OriginalPawnData` | 仅本地：原始 Pawn 数据备份 (用于归还) |

---

### Source/TalentTrade/Patches/RootUpdatePatches.cs

**类**: `RootUpdatePatches`

Harmony Postfix 补丁，挂在 `Root.Update()` 上。每帧调用 `TalentTradeManager.Update()`。

这是整个 Mod 的心跳。没有这个补丁，所有轮询、发送、消息处理都不会运行。

---

### Source/TalentTrade/Patches/ServerTabPatches.cs

**辅助类**: `ServerTabAccess` — 通过反射访问 `ServerTab` 的私有字段 `activeTab` 和 `tabList`

**补丁 1**: `ServerTab_Ctor_Patch` (构造函数 Postfix)
- 向 Phinix 的 Tab 列表追加"人才贸易"标签页
- 记录 Tab 索引到 `TalentTradeTab.Instance.TabIndex`

**补丁 2**: `ServerTab_DoWindowContents_Patch` (Postfix)
- 当前 Tab 是人才贸易时，绘制 `TalentTradeTab.Instance.Draw()`

**补丁 3**: `ServerTab_OnAcceptKeyPressed_Patch` (Prefix)
- 非聊天 Tab 时阻止 Enter 键触发聊天发送

---

### Source/TalentTrade/Patches/ChatFilterPatches.cs

4 个 Harmony 补丁，作用于 `PhinixClient.GUI.ChatMessageList`，隐藏 `PHXTT|` 协议消息：

| 补丁 | 目标方法 | 策略 |
|------|----------|------|
| `MessageReceived_Patch` | `ChatMessageReceivedEventHandler` | Prefix：协议消息返回 false 跳过 |
| `RecalculateMessageRects_Patch` | `recalculateMessageRects` | Prefix：从 messages 和 filteredMessages 列表中移除 |
| `ReplaceWithBuffer_Patch` | `ReplaceWithBuffer` | Prefix 替换：重建消息列表时过滤 |
| `DrawChatMessage_Patch` | `drawChatMessage` | Prefix：跳过绘制协议消息 |

所有补丁使用 `TalentTradeProtocol.IsProtocolMessage()` 作为过滤判据。

---

### Source/TalentTrade/UI/TalentTradeTab.cs

**类**: `TalentTradeTab` (单例 `Instance`)

人才贸易的主 UI 面板，包含三个子标签页。

| 成员 | 说明 |
|------|------|
| `TabIndex` | 由 ServerTab 补丁设置的 Tab 索引 |
| `SubTab` 枚举 | DirectTrade / Market / Rental |
| `Draw(Rect)` | 入口：未登录显示提示，已登录显示子标签栏 + 内容区 |
| `DrawDirectTradePanel` | ⬜ 桩 (Phase 4) |
| `DrawMarketPanel` | ⬜ 桩 (Phase 3) |
| `DrawRentalPanel` | ⬜ 桩 (Phase 5) |

---

### Languages/\*/Keyed/Keyed.xml

中英文翻译文件，共 91 个翻译键，覆盖：

- Tab / 子标签名称
- 设置项
- 通用 UI (确认、取消、刷新、搜索)
- 市场相关 (上架、下架、购买、价格格式)
- 直接交易相关 (请求、接受、报价、锁定)
- 租借相关 (上架、租借、归还、到期、死亡复活)
- Pawn 传输兼容性报告 (种族/装备/植入体/特性的兼容检查)
- 通知消息
