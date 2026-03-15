# 开发日志

## 阶段总览

| 阶段 | 内容 | 状态 |
|------|------|------|
| Phase 1 | 骨架搭建 | ✅ 完成 |
| Phase 2 | Pawn 序列化 + Def 兼容性交换 | ✅ 完成 |
| Phase 3 | 公共市场 | ✅ 完成 |
| Phase 4 | 直接交易 | ✅ 完成 |
| Phase 5 | 租借系统 | ✅ 完成 |
| Phase 6 | 安全加固 | ✅ 完成 |

---

## Phase 1: 骨架搭建 ✅

建立项目结构，实现基础通信框架。

- `About.xml` — Mod 元数据、依赖声明
- `TalentTradeMod.cs` — Mod 入口、Harmony 初始化
- `TalentTradeSettings.cs` — 设置持久化
- `TalentTradeTransport.cs` — HTTP 中继传输层 (发送/轮询/压缩)
- `TalentTradeProtocol.cs` — 协议编解码 (25 种消息类型)
- `TalentTradeManager.cs` — 核心管理器 (线程安全状态、消息路由、Blob 拼装)
- `RootUpdatePatches.cs` — 主循环钩子
- `ServerTabPatches.cs` — Phinix Tab 注入
- `ChatFilterPatches.cs` — 聊天协议消息过滤
- `TalentTradeTab.cs` — 三子标签 UI 骨架
- 数据模型: `PawnSummary`、`MarketListing`、`DirectTrade`、`RentalContract`
- 中英文翻译文件 (91 个翻译键)
- `TalentTrade.csproj` — SDK-style 项目文件

---

## Phase 2: Pawn 序列化 + Def 兼容性交换 ✅

实现 Pawn 的完整导出/导入，以及交易前的 Mod 兼容性检查。

- `PawnSerializer.cs` — Pawn → ScribeSaver.DebugOutputFor → XML → GZip → Base64
- `PawnDeserializer.cs` — Base64 → GZip → XML 安全校验 → ScribeExtractor 反序列化 → PostProcess
- `DefManifestHelper.cs` — 收集本地 Def 清单，交易前交换检测缺失 mod
- PostProcess 流程: ID 重生成 (Pawn/装备/服装/库存/hediff/gene)、意识形态修复、Job/Stance 重置
- `PawnTextureAtlasGCPatch.cs` — 修复原版反序列化时 TextureAtlas GC 的 KeyNotFoundException

---

## Phase 3: 公共市场 ✅

实现固定价格的 Pawn 买卖。

- `MarketPanel.cs` — 市场 UI (列表浏览、搜索过滤、上架表单、购买确认)
- Manager 中的 HandleMarketBuy / HandleMarketSell / HandleMarketPaid / HandleMarketSync
- 卖家侧: 选择 Pawn → 设定价格 → 上架 (Pawn 脱离地图暂存)
- 买家侧: 浏览列表 → 购买 → 接收 Pawn (空投舱送达)
- 市场同步: 新上线玩家请求 msync，在线卖家回复当前挂牌

---

## Phase 4: 直接交易 ✅

实现 1v1 协商式交易。

- `DirectTradePanel.cs` — 在线用户列表 + 活跃交易面板
- `DirectTradeWindow.cs` — 交易协商窗口 (左右分栏双方 offer 视图)
- Manager 中的所有 HandleTrade* 方法
- 双方锁定确认机制 (两人都锁定后才执行交换)
- Pawn/物品的暂存与释放
- `TradeOfferSerializer.cs` — TradeOffer ↔ Base64 序列化

---

## Phase 5: 租借系统 ✅

实现 Pawn 的定期租借，含自动归还和死亡复活。

- `RentalPanel.cs` — 租借 UI (出租列表、我的出租、我租的)
- Manager 中的所有 HandleRental* 方法
- 租借到期检测: 每帧检查 ExpiryTick，到期自动触发归还
- 归还机制: 空投仓将 Pawn 送回出租方
- 死亡处理: Pawn 死亡 → 通知出租方 → 从缓存复活 → 空投仓归还
- `TalentTradeGameComponent.cs` — 每存档持久化，管理 Pawn 备份/恢复

---

## Phase 6: 安全加固 ✅ (2026.3.14)

Relay 服务器和客户端的安全增强。

### API Key 认证
- relay.py: `X-Api-Key` header 校验，不匹配返回 403
- Transport.cs: 发送时附加 API Key header

### IP 速率限制
- relay.py: 每 IP 每分钟最多 60 次 POST，超限返回 429
- 后台线程定期清理过期 bucket

### HMAC 消息签名
- relay.py: `_verify_signature()` 验证 `X-Signature` header
- Transport.cs: `ComputeHmacSha256()` 对 body 签名
- relay-tester.html: Web Crypto API 计算 HMAC
- 签名算法: HMAC-SHA256，密钥与 API Key 独立

### Pawn 数据校验
- PawnDeserializer.cs 增强:
  - 大小限制: 4MB Base64 / 2MB XML
  - XXE 防护: 禁止 DOCTYPE/ENTITY + XmlResolver=null
  - 结构校验: 必须含 Pawn Class/def/kindDef，XML 深度 ≤50，节点 ≤10000
  - 数据校验: humanlike 种族检查，技能等级钳制 (max 20)

### 消息体限制
- relay.py: 单条消息最大 512KB，超限返回 413
