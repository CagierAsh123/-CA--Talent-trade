# 开发阶段

## 阶段总览

| 阶段 | 内容 | 状态 |
|------|------|------|
| Phase 1 | 骨架搭建 | ✅ 完成 |
| Phase 2 | Pawn 序列化 + Def 兼容性交换 | ⬜ 待开发 |
| Phase 3 | 公共市场 | ⬜ 待开发 |
| Phase 4 | 直接交易 | ⬜ 待开发 |
| Phase 5 | 租借系统 | ⬜ 待开发 |
| Phase 6 | 打磨与测试 | ⬜ 待开发 |

---

## Phase 1: 骨架搭建 ✅

建立项目结构，实现基础通信框架。

**完成内容**:
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
- 中英文翻译文件
- `TalentTrade.csproj` — 项目文件 (编译通过，0 错误 0 警告)

---

## Phase 2: Pawn 序列化 + Def 兼容性交换

实现 Pawn 的完整导出/导入，以及交易前的 Mod 兼容性检查。

**待实现**:
- `PawnSerializer.cs` — 通过 RimWorld Scribe 系统导出完整 Pawn XML → GZip → Base64
- `PawnDeserializer.cs` — Base64 → GZip 解压 → XML → 在目标地图生成 Pawn
- `DefManifestHelper.cs` — 收集本地已加载的 Def 清单 (种族、装备、Hediff、特性)
- `HandleDefManifest` / `HandleDefAck` — 实现 Def 交换逻辑
- Pawn 传输前的兼容性报告 UI (使用已有的 `transferRace*`、`transferEquip*` 等翻译键)
- `PawnSummary` 从实际 Pawn 对象提取摘要的工厂方法

**关键设计**:
- 导出时使用 `Scribe.saver` 将 Pawn 序列化为 XML 字符串
- 导入时使用 `Scribe.loader` 反序列化，然后通过空投仓 (Drop Pod) 降落到地图
- Def 清单交换在交易确认前自动进行，不兼容的 Def 项会在报告中标注

---

## Phase 3: 公共市场

实现固定价格的 Pawn 买卖。

**待实现**:
- `MarketPanel.cs` — 市场 UI (列表浏览、搜索、上架表单、购买确认)
- Manager 中的 `HandleMarketBuy` / `HandleMarketSell` / `HandleMarketPaid` / `HandleMarketSync`
- 卖家侧: 选择 Pawn → 设定价格 → 上架 (Pawn 脱离地图暂存)
- 买家侧: 浏览列表 → 购买 → 扣除银子 → 接收 Pawn
- 银子扣除/增加的游戏内逻辑
- 市场同步: 新上线玩家请求 `msync`，在线卖家回复当前挂牌

**交易流程**:
```
卖家: mlist → 全体收到挂牌
买家: mbuy → 卖家收到购买请求
卖家: msell (含 Pawn 数据) → 买家收到 Pawn
买家: mpaid → 卖家确认收款
```

---

## Phase 4: 直接交易

实现 1v1 协商式交易。

**待实现**:
- `DirectTradePanel.cs` — 交易 UI (在线用户列表、交易窗口、双方报价、锁定确认)
- Manager 中的所有 `HandleTrade*` 方法
- 交易窗口: 左右分栏显示双方报价，支持添加/移除 Pawn 和银子
- 双方锁定确认机制 (两人都点"锁定"后才执行)
- Pawn/物品的暂存与释放

**交易流程**:
```
A: treq → B 收到交易请求弹窗
B: tacc → 双方进入协商
A/B: toff → 更新报价 (可多次)
A: tlock → A 锁定
B: tlock → B 锁定 → 双方都锁定，触发执行
A: texe (含 Pawn 数据) → B 收到 A 的 Pawn
B: texe (含 Pawn 数据) → A 收到 B 的 Pawn
```

---

## Phase 5: 租借系统

实现 Pawn 的定期租借，含自动归还和死亡复活。

**待实现**:
- `RentalPanel.cs` — 租借 UI (出租列表、我的出租、我租的)
- Manager 中的所有 `HandleRental*` 方法
- 租借到期检测: 每帧检查 `ExpiryTick`，到期自动触发归还
- 归还机制: 通过空投仓将 Pawn 送回出租方
- 死亡处理: Pawn 死亡时 → 通知出租方 → `ResurrectionUtility.TryResurrect` 复活 → 空投仓归还
- 押金/日租金的银子结算

**租借流程**:
```
出租方: rlist → 全体收到出租信息
承租方: rrent → 出租方收到租借请求
出租方: rconf (含 Pawn 数据) → 承租方收到 Pawn
  ... 租借期间 ...
到期: rexp (含 Pawn 数据) → 出租方收回 Pawn
或死亡: rdead → 出租方收到通知 → rrev → 复活归还
```

---

## Phase 6: 打磨与测试

- 错误处理与边界情况
- 网络断线重连恢复
- UI 美化 (Pawn 头像预览、技能/特性详情弹窗)
- 性能优化 (大量挂牌时的列表虚拟化)
- 多语言校对
- 与其他 Mod 的兼容性测试
