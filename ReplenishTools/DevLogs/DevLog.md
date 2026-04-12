Goals: 
- Regenerate the number of red tools over time
  - The regeneration speed should be based on the total tool capacity (straight pin will regenerate multiple times faster than the pimpilo)
- Decrease the red tool capacity by half across all red-tools
- (Optional) Double tool replenishment speed after hitting an enemy for the next 1.5s

Codebase exploration:
- Tools are called `ToolItems` in the code
- ToolItems have a `type` field that specifies the tools color using the `ToolItemType` enum.
  - Red tools are identified with `ToolItemType.Red`
- A capacity of a tool is accessable through the `BaseStorageAmount` getter function.
  - This property is read-only
- The current number of available tools is tracked in the struct `ToolItemsData.Data.AmountLeft`
- `AmountLeft` can be accessed throughthe property `ToolItemsData.Data ToolItems.SavedData`
  - Is read & write, both of which call upon `PlayerData.instance` `GetToolData()` and `SetToolData(string, ToolItemsData.Data)` respectively.
- Currenly equiped tools can be obtained by calling `ToolItemManager.GetCurrentEquippedTools()`
- Tools are replenished using `ToolItemManager.TryReplenishTools(bool, ReplenishMethod)`
- Whether replenishment is required is checked based on if the tool is a skill or not or if the `BaseStorageAmount` is greater than 0.

The ReplenishMethod enum
```
    public enum ReplenishMethod
    {
        Bench,
        QuickCraft,
        BenchSilent
    }
```