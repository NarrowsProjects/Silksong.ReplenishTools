Goals: 
- Regenerate the number of red tools over time
  - The regeneration speed should be based on the total tool capacity (straight pin will regenerate multiple times faster than the pimpilo)
- ~~Decrease the red tool capacity by half across all red-tools~~
  - ~~Completed, with the caviat of the cost of all tools being doubled~~
- (Optional) Double tool replenishment speed after hitting an enemy for the next 1.5s

Codebase exploration:
- `ToolItem`:
  - Tools are called `ToolItems` in the code
  - ToolItems have a `type` field that specifies the tools color using the `ToolItemType` enum.
    - Red tools are identified with `ToolItemType.Red`
  - A capacity of a tool is accessable through the `BaseStorageAmount` getter function.
    - This property is read-only
  - The current number of available tools is tracked in the struct `ToolItemsData.Data. AmountLeft`
  - `AmountLeft` can be accessed throughthe property `ToolItemsData.Data ToolItems.SavedData`
    - Is read & write, both of which call upon `PlayerData.instance` `GetToolData()` and  `SetToolData(string, ToolItemsData.Data)` respectively.
  - `ToolItem` has the function `TryReplenishSingle(bool deReplenish, float costIn, out float costOut, out int reserveCost)`
    - It doesn't seem to do anything other than calculate the cost rather than actually replenishing anything. Additionally, `doReplenish` is not used.
    - `ToolItemStatesLiquid` has the same function, except the function has additional functionallity here and `doReplenish` is used
    - My guess is that this class are specifically made for flea brew and plasmium phial. As they don't use shell shards
  - Images, sprites and such are controlled using the `ToolItemState` Class
  - ToolItems calculate the cost of replenishing tools based on an enum entry every tool selects called `replenishUsages`
    - I'm guessing this is why the sting shard has a different cost depending on if you use one vs two of them (7 vs 13)
  - A field called `ReplenishUsageMultiplier` stores
- `ToolItemManager`
  - Currenly equiped tools can be obtained by calling `ToolItemManager.GetCurrentEquippedTools()`
  - Tools are replenished using `ToolItemManager.TryReplenishTools(bool, ReplenishMethod)` which calls `ToolItem.TryReplenishSingle()` twice
    - It tracks the current amount of currency (per type) the player has by creating two coppies:
      - `_endingCurrencyAmounts` (Array<int>)
      - `_startingCurrencyAmounts` (Array<int>)
      - Both are set using `CurrencyManager.GetCurrencyAmount(type)`
      - `_endingCurrencyAmounts` is changed in the function and is subtracted from `_startingCurrencyAmount`. The result is then passed to this method `CurrencyManager.TakeCurrency(int amount, CurrencyType type, bool showCounter = true);`
    - once with `deReplenish` as false (my guess would be to compute if this action is feasible or not. If it is)
    - once with `deReplenish` as false IF the previous one returned true
  - The
  - Whether replenishment is required is checked based on if the tool is a skill or not or if the `BaseStorageAmount` is greater than 0.
  - Replacement of a tool is canceled if this check rules false:
    - _endingCurrencyAmounts[(int)item.ReplenishResource] - outCost <= -0.5f

- Enemy Interaction
  - The class `EnemyHitRegular` has the public method called `ReceiveHitEffect`, which creates a new HitInstance instance (object initializer syntax) with a direction (which is a float??) and an attack type. In this case the attack type is generic, which is likely a normal strike with the needle.
    - So, direction has nothing to do with direction, it is just the magnitude/force with which something (enemy or hornet) is moved. The direction is based on the hit type (sting shard moves towards center of the trap for instance)
  - HitInstance has an internal enum called targetType, which contains the options: `Regular`, `Corpse`, `BouncePod`, `Currency`
  - I can probably trigger the double tool replenishment regeneration by just checking if `EnemyHitRegular.ReceiveHitEffect` has been triggered.

- Ragpelts lag the game when mod is turned on. I have to investigate this.
  - The logs show erors about applying linear velocity to something.
  - the class `PersistentEnemyItemDrop` has a function called `DropItem(bool fling)`.
    - It appears to control the movement of the items
    - If `fling` is false and `startedDeadSpawnPoint` is true (it needs to be cast to a bool first).
      - This might be pilgrim shawls as those 'drop' on the pilgrim corpse.
    - Rag pelts might have `fling` = true as the pelts explode outwards
  - nothing found regarding linear velocity

The ReplenishMethod enum
```
    public enum ReplenishMethod
    {
        Bench,  (crafting when sitting on a bench)
        QuickCraft, (architect's craft bind (probably))
        BenchSilent  (I think this is what happens upon quiting to menu or upon respawn)
    }
```

```
    public enum ReplenishUsages
    {
        Percentage,
        OneForOne,
        Custom
    }
```