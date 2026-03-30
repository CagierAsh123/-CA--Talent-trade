# 租借链路剩余缺口

本文档记录当前 Talent Trade 租借系统在代码审计中确认的剩余问题。它们不属于“市场 / 直交易扩展到动物、机械体、囚犯”的核心实现，但会影响租借生命周期的完整性与可预测性。

---

## 1. 当前已经成立的部分

以下链路目前是存在的：

- 租借入口已重新收紧为仅普通殖民者
- 远端收到租借列表时，也会再次按 `CanRentPawn(summary)` 过滤
- owner 收到租借请求后，会用本地缓存的 `OriginalPawnData` 给 renter 发 pawn
- renter 手动归还时，owner 会忽略 renter 数据，直接用自己缓存的 `OriginalPawnData` 恢复

也就是说，当前租借系统更接近“借出一份快照”，而不是“把同一个活体 pawn 的实时状态借出再带状态归还”。

---

## 2. 已确认的主要缺口

## 2.1 renter 侧没有绑定“当前租来的 pawn 身份”

现状：

- `RentalContract` 里有 `RentedPawnThingID`
- 但代码里没有真正写入 / 读取它
- `HandleRentalConfirm(...)` 只负责把 pawn 生成出来，没有把新生成 pawn 的 ThingID 绑定回合同

影响：

- renter 后续无法稳定找到“我现在租来的那个 pawn”
- manual return / expiry / death 都缺少可靠的本地目标

---

## 2.2 手动归还不会主动清理 renter 侧地图上的 pawn

现状：

- `ReturnRentedPawn(...)` 当前只发送 `BuildRentalReturn(rentalId, localUuid, "")`
- 然后直接把本地合同移除
- 没有在 renter 地图上 despawn / remove / destroy 当前租来的 pawn

影响：

- owner 会从缓存恢复原 pawn
- renter 侧很可能仍保留一份租来的副本
- 容易出现“owner 收回了，renter 本地还留着”的重复状态

这也是目前最需要优先补的租借缺口之一。

---

## 2.3 到期自动返还缺少主动驱动逻辑

现状：

- 协议里有 `BuildRentalExpiry(...)`
- 管理器里也有 `HandleRentalExpiry(...)`
- 但 `TalentTradeManager.Update()` 当前只做：
  - `CheckPurchaseTimeouts()`
  - `ExpireOldListings()`
  - `HeartbeatMyListings()`
- 没有任何 active rental 的定时扫描逻辑

影响：

- 合同虽然记录了 `ExpiryTick`
- 但没有代码在游戏运行中检查“是否已到期”
- 因此自动返还大概率不会自己触发

---

## 2.4 死亡上报 / revive 回传链路只有“接收端”，缺少“触发端”

现状：

- 协议里有：
  - `BuildRentalDead(...)`
  - `BuildRentalRevive(...)`
- 管理器里有：
  - `HandleRentalDead(...)`
  - `HandleRentalRevive(...)`
- 但没有看到 renter 侧主动检测“租借 pawn 死亡”并发送 `rdead` 的驱动代码

影响：

- owner revive 逻辑虽然写了
- 但前置事件未必会发出去
- 实战里可能根本不会进入 revive 回传链路

---

## 2.5 当前租借语义仍然是“owner 缓存优先”，不是“状态归还”

现状：

- `HandleRentalReturn(...)` 明确忽略 renter 传回的数据
- owner 始终恢复 `OriginalPawnData`
- 这意味着 renter 期间对 pawn 的变化不会回到 owner 侧

影响：

以下变化默认都会丢失，除非后续重构：

- 受伤 / 治疗结果
- 技能成长
- 装备变化
- 背包变化
- 情绪 / needs / 部分状态变化

这未必是 bug，但必须明确为设计语义；否则玩家会误以为租借是“借出原 pawn 再原样归还”。

---

## 3. 建议的最小修复顺序

建议按最小代价顺序处理：

### 第一步：给 renter 合同绑定真实 pawn 身份

在 `HandleRentalConfirm(...)` 生成 pawn 后：

- 把 `pawn.ThingID` 或其他可稳定定位的标识写入 `contract.RentedPawnThingID`

目标：

- 后续 return / expiry / death 能定位正确 pawn

### 第二步：补“renter 侧本地清理”

在 manual return / expiry / death 发生时：

- 先找到 renter 侧当前租来的 pawn
- 再明确执行 despawn / remove
- 最后再移除合同

目标：

- 避免 owner 恢复成功后，renter 本地仍残留副本

### 第三步：给 active rental 增加 tick 扫描

建议在 `TalentTradeManager.Update()` 或一个更合适的 game tick 驱动里增加：

- active rental 到期检查
- rented pawn 死亡检查
- 合同状态异常检查

目标：

- 让 `rexp` / `rdead` 不再只是“有 handler 但没人触发”

### 第四步：明确租借语义

必须二选一：

1. **快照租借**
   - owner 永远恢复自己缓存的原始版本
   - renter 侧变化全部丢弃
   - 文档里明确写清楚

2. **状态归还租借**
   - renter 归还时序列化当前 pawn
   - owner 用 renter 发回的数据恢复
   - 需要重新设计 return / revive / 安全校验

如果近期目标只是把租借做稳定，建议先选“快照租借”，把行为定义写清楚，再补齐本地清理与自动驱动。

---

## 4. 与本轮扩展任务的关系

本轮主目标是：

- 市场支持：殖民者、动物、机械体、囚犯
- 直交易支持：殖民者、动物、机械体、囚犯
- 租借限制回：普通殖民者 only

所以这些租借缺口：

- **不阻塞** 当前动物 / 机械体 / 囚犯进入市场与直交易
- **会阻塞** 租借生命周期被视为“完整可靠功能”

换句话说：

- 作为本轮“扩展交易对象”任务，它们属于后续修复项
- 作为“租借系统是否已经完全稳定”的问题，它们是当前主要风险来源

---

## 5. 推荐结论

当前建议是：

1. 先把本轮扩展对象支持验收完
2. 租借仅保留普通殖民者入口
3. 把租借生命周期缺口单独作为下一轮修复任务

如果要继续做租借修复，优先级建议如下：

1. renter 本地副本清理
2. active rental tick 驱动
3. death / revive 触发链
4. 统一定义“快照租借 / 状态归还租借”语义
