using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GlobalSettings;

[BepInPlugin("com.NarrowsProjects.ReplenishTools", "Replenish tools over time", "1.0.0")]
public class ReplenishTools : BaseUnityPlugin
{
    // Straight pin has a capacity of 24 at max pouches and should take 5 seconds to replenish a single pin
    // Divided by 2 because the mod reduces it's capacity by half.
    // Higher capacity = faster replenish rate;
    private const float BaseRegenSeconds = 5f * 24f / 2;

    private readonly Dictionary<string, float> _regenAccumulator = new Dictionary<string, float>();

    internal static bool IsRedTool(ToolItem tool) => tool?.Type == ToolItemType.Red;
    internal static bool IsPercentageReplenish(ToolItem tool) => tool?.ReplenishUsage == ToolItem.ReplenishUsages.Percentage;
    internal static bool CurrencyIsDeductable(ToolItem.ReplenishResources currencyType) =>
        currencyType != ToolItem.ReplenishResources.None;

    public void Awake()
    {
        Logger.LogInfo("Loading ReplenishTools mod");
        Harmony harmony = new Harmony("com.NarrowsProjects.ReplenishTools");
        harmony.PatchAll();
    }

    public void Start()
    {
        StartCoroutine(RegenLoop());
    }

    // Accumulates fractional regen progress for each equiped red tool and returns how many
    // tools to replenish this tick.
    private int AccumulateRegen(ToolItem tool, float deltaTime)
    {
        int capacity = ToolItemManager.GetToolStorageAmount(tool);
        float rate = (float)capacity / BaseRegenSeconds;

        if (!_regenAccumulator.TryGetValue(tool.name, out float accumulated))
        {
            accumulated = 0f;
        }

        accumulated += rate * deltaTime;
        int toolReplenishCount = Mathf.FloorToInt(accumulated);
        _regenAccumulator[tool.name] = accumulated - toolReplenishCount;
        return toolReplenishCount;
    }

    internal bool ReplenishTool(ToolItem tool, int toolReplenishCount)
    {
        if (!IsRedTool(tool) || toolReplenishCount <= 0)
        {
            return false;
        }

        ToolItemsData.Data data = PlayerData.instance.GetToolData(tool.name);
        int capacity = ToolItemManager.GetToolStorageAmount(tool);

        if (data.AmountLeft >= capacity)
        {
            _regenAccumulator[tool.name] = 0f;
            return false;
        }

        int toAdd = Mathf.Min(toolReplenishCount, capacity - data.AmountLeft);

        for (int i = 0; i < toAdd; i++)
        {
            if (!CanAffordReplenish(tool))
            {
                break;
            }

            DeductReplenishCost(tool);
            data.AmountLeft++;
        }

        PlayerData.instance.SetToolData(tool.name, data);
        Logger.LogInfo($"[Regen] {tool.name}: {data.AmountLeft}/{capacity}");
        return true;
    }

    internal int ComputeReplenishCost(ToolItem tool)
    {
        float baseValue;

        switch (tool.ReplenishUsage)
        {
            case ToolItem.ReplenishUsages.Percentage:
                baseValue = 1f / (float)tool.BaseStorageAmount * (float)Gameplay.ToolReplenishCost;
                break;

            case ToolItem.ReplenishUsages.OneForOne:
                baseValue = 1f;
                break;

            case ToolItem.ReplenishUsages.Custom:
                baseValue = 0f;
                break;

            default:
                baseValue = 0f;
                break;
        }

        return Mathf.RoundToInt(baseValue * tool.ReplenishUsageMultiplier);
    }

    internal bool CanAffordReplenish(ToolItem tool)
    {
        if (!CurrencyIsDeductable(tool.ReplenishResource))
        {
            return true;
        }

        int cost = ComputeReplenishCost(tool);
        int currentAmount = CurrencyManager.GetCurrencyAmount((CurrencyType)tool.ReplenishResource);
        return currentAmount - cost > -0.5f;
    }

    internal void DeductReplenishCost(ToolItem tool)
    {
        int cost = ComputeReplenishCost(tool);
        if (cost > 0 && CurrencyIsDeductable(tool.ReplenishResource))
        {
            CurrencyManager.TakeCurrency(cost, (CurrencyType)tool.ReplenishResource, showCounter: true);
        }
    }

    private IEnumerator RegenLoop()
    {
        float lastTime = Time.time;
        while (true)
        {
            yield return null;
            float now = Time.time;
            float delta = now - lastTime;
            lastTime = now;

            TryRegenRedTools(delta);
        }
    }

    private void TryRegenRedTools(float deltaTime)
    {
        if (PlayerData.instance == null)
        {
            return;
        }

        string crestId = PlayerData.instance.CurrentCrestID;
        List<ToolItem> equippedTools = ToolItemManager.GetEquippedToolsForCrest(crestId);
        if (equippedTools == null)
        {
            return;
        }

        bool anyChanged = false;

        foreach (ToolItem tool in equippedTools)
        {
            if (!IsRedTool(tool))
            {
                continue;
            }

            int toolReplenishCount = AccumulateRegen(tool, deltaTime);
            anyChanged |= ReplenishTool(tool, toolReplenishCount);
        }

        if (anyChanged)
        {
            ToolItemManager.ReportAllBoundAttackToolsUpdated();
            ToolItemManager.SendEquippedChangedEvent(force: true);
        }
    }

    [HarmonyPatch(typeof(ToolItem), nameof(ToolItem.BaseStorageAmount), MethodType.Getter)]
    static class PatchToolCapacity
    {
        static void Postfix(ToolItem __instance, ref int __result)
        {
            if (IsRedTool(__instance))
            {
                __result = Mathf.Max(1, Mathf.CeilToInt(__result * 0.5f));
            }
        }
    }

    // Due to the order of operations in calculating the cost of replenishing red tools that are replenished by percentage
    // inversely proportional to BaseStorageAmount.
    // To prevent this from doubling the cost of all red tools the ReplenishUsageMultiplier will be halfed
    [HarmonyPatch(typeof(ToolItem), nameof(ToolItem.ReplenishUsageMultiplier), MethodType.Getter)]
    static class PatchReplenishMultiplier
    {
        static void Postfix(ToolItem __instance, ref float __result)
        {
            if (IsRedTool(__instance) && IsPercentageReplenish(__instance))
            {
                __result *= 0.5f;
            }
        }
    }
}
