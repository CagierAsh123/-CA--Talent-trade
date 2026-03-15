# 通信协议

> 最后更新: 2026-03-15

## 概述

所有消息均为管道符 `|` 分隔的纯文本字符串，前缀固定为 `PHXTT|v1|<类型码>`。

消息通过 HTTP 中继服务器在玩家间传递，房间名为 `talent-trade`。

## 消息格式

```
PHXTT|v1|<type>|field1|field2|field3|...
```

- `PHXTT` — 固定前缀，用于识别协议消息
- `v1` — 协议版本号
- `<type>` — 消息类型码 (见下表)
- 后续字段因消息类型而异

## 字段编码规则

- 普通字段 (ID、UUID、数字): 直接写入
- 含特殊字符的文本 (玩家名、Pawn 名): Base64 编码后写入，使用 `EncodeField()` / `DecodeField()`
- 大数据 (完整 Pawn XML): GZip 压缩 → Base64 编码，若超长则拆分为 BlobPart

## 传输层安全

| 机制 | 实现位置 | 说明 |
|------|----------|------|
| API Key 认证 | `X-Api-Key` header | relay.py 校验，不匹配返回 403 |
| HMAC 签名 | `X-Signature` header | HMAC-SHA256(body)，relay.py 验证完整性 |
| 速率限制 | relay.py | 60 POST/分钟/IP，超限返回 429 |
| 消息体限制 | relay.py | 单条最大 512KB，超限返回 413 |

## 消息类型一览

### 公共市场 (Market)

| 类型码 | 枚举值 | 方向 | 格式 | 说明 |
|--------|--------|------|------|------|
| `mlist` | MarketList | 卖家→全体 | `PHXTT\|v1\|mlist\|listingId\|sellerUuid\|b64PawnSummary\|priceSilver\|b64SellerName` | 上架出售 |
| `mdel` | MarketDelist | 卖家→全体 | `PHXTT\|v1\|mdel\|listingId\|sellerUuid` | 下架 |
| `mbuy` | MarketBuy | 买家→卖家 | `PHXTT\|v1\|mbuy\|listingId\|buyerUuid\|b64BuyerName` | 购买请求 |
| `msell` | MarketSell | 卖家→买家 | `PHXTT\|v1\|msell\|listingId\|sellerUuid\|buyerUuid\|b64CompressedPawnData` | 发送 Pawn 数据 |
| `mpaid` | MarketPaid | 买家→卖家 | `PHXTT\|v1\|mpaid\|listingId\|buyerUuid\|b64CompressedSilverItems` | 确认付款 (含银子物品数据) |
| `msync` | MarketSync | 任意→全体 | `PHXTT\|v1\|msync\|requesterUuid` | 请求同步当前列表 |

### 直接交易 (Direct Trade)

| 类型码 | 枚举值 | 方向 | 格式 | 说明 |
|--------|--------|------|------|------|
| `treq` | TradeRequest | A→B | `PHXTT\|v1\|treq\|tradeId\|initiatorUuid\|targetUuid\|b64InitiatorName` | 发起交易请求 |
| `tacc` | TradeAccept | B→A | `PHXTT\|v1\|tacc\|tradeId\|responderUuid` | 接受交易 |
| `trej` | TradeReject | B→A | `PHXTT\|v1\|trej\|tradeId\|responderUuid` | 拒绝交易 |
| `toff` | TradeOffer | 任一方→对方 | `PHXTT\|v1\|toff\|tradeId\|senderUuid\|b64OfferJson` | 更新报价 |
| `tlock` | TradeLock | 任一方→对方 | `PHXTT\|v1\|tlock\|tradeId\|senderUuid` | 锁定确认 |
| `texe` | TradeExecute | 双方 | `PHXTT\|v1\|texe\|tradeId\|senderUuid\|b64CompressedPawnData\|b64CompressedItems` | 执行交易 (发送 Pawn + 物品) |
| `tcan` | TradeCancel | 任一方→对方 | `PHXTT\|v1\|tcan\|tradeId\|senderUuid` | 取消交易 |

### 租借 (Rental)

| 类型码 | 枚举值 | 方向 | 格式 | 说明 |
|--------|--------|------|------|------|
| `rlist` | RentalList | 出租方→全体 | `PHXTT\|v1\|rlist\|rentalId\|ownerUuid\|b64PawnSummary\|pricePerDay\|maxDays\|deposit\|b64OwnerName` | 上架出租 |
| `rdel` | RentalDelist | 出租方→全体 | `PHXTT\|v1\|rdel\|rentalId\|ownerUuid` | 下架 |
| `rrent` | RentalRent | 承租方→出租方 | `PHXTT\|v1\|rrent\|rentalId\|renterUuid\|rentDays\|b64RenterName` | 租借请求 |
| `rconf` | RentalConfirm | 出租方→承租方 | `PHXTT\|v1\|rconf\|rentalId\|ownerUuid\|renterUuid\|b64CompressedPawnData` | 确认并发送 Pawn |
| `rret` | RentalReturn | 承租方→出租方 | `PHXTT\|v1\|rret\|rentalId\|renterUuid\|b64CompressedPawnData` | 归还 Pawn |
| `rexp` | RentalExpiry | 承租方→出租方 | `PHXTT\|v1\|rexp\|rentalId` | 到期自动归还 |
| `rdead` | RentalDead | 承租方→出租方 | `PHXTT\|v1\|rdead\|rentalId\|renterUuid` | Pawn 死亡通知 |
| `rrev` | RentalRevive | 出租方→承租方 | `PHXTT\|v1\|rrev\|rentalId\|ownerUuid\|b64CompressedPawnData` | 复活并归还 |

### Def 兼容性交换

| 类型码 | 枚举值 | 方向 | 格式 | 说明 |
|--------|--------|------|------|------|
| `defs` | DefManifest | A→B | `PHXTT\|v1\|defs\|sessionId\|senderUuid\|targetUuid\|b64CompressedDefList` | 发送本地 Def 清单 |
| `dack` | DefAck | B→A | `PHXTT\|v1\|dack\|sessionId\|senderUuid\|b64MissingDefs` | 回复缺失的 Def 列表 |

### 大数据分片 (Blob)

| 类型码 | 枚举值 | 格式 | 说明 |
|--------|--------|------|------|
| `blob` | BlobPart | `PHXTT\|v1\|blob\|blobId\|partIndex\|totalParts\|b64PartData` | 大数据分片 |

分片流程：
1. 原始数据 → GZip 压缩 → Base64 编码
2. 按固定长度切片，每片构造一条 `blob` 消息
3. 接收方收集所有分片 (`partIndex` 从 0 开始)
4. 全部到齐后拼接还原为完整字符串
5. 还原后的字符串作为普通协议消息重新处理

## 去重机制

`TalentTradeManager` 使用 `ProcessedProtocolKeys` 集合进行消息去重。去重键的构造规则：

```
dedupKey = 消息类型枚举名 + "|" + parts[3]
```

其中 `parts[3]` 通常是该消息的唯一业务 ID (listingId / tradeId / rentalId / blobId)。

若消息字段不足 4 个，则不去重。

## 协议版本

当前版本: `v1`

`TryParse()` 会校验版本字段，非 `v1` 的消息将被丢弃。未来若需升级协议，修改版本号并在解析器中添加分支即可。

## Relay 服务器配置

| 配置项 | 值 |
|--------|-----|
| 地址 | `http://114.55.115.143:8080` |
| 房间名 | `talent-trade` |
| API Key | `tt-9f3a7c2e1b4d6058e7a2c9d4f1b3e5a8` |
| Signing Key | `tt-sign-b7e2f4a19c3d5068d1e4a7b2c8f0e3d6` |
| 轮询间隔 | 700ms |
| 发送间隔 | 80ms |
| 请求超时 | 5000ms |
| 每次拉取上限 | 200 条 |
| 首次连接历史 | 最近 20 分钟 |
