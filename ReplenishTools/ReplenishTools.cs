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
    private const float RegenIntervalSeconds = 5f;

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

    internal bool ReplenishTool(ToolItem tool)
    {
        if (!IsRedTool(tool))
        {
            return false;
        }

        ToolItemsData.Data data = PlayerData.instance.GetToolData(tool.name);
        int capacity = ToolItemManager.GetToolStorageAmount(tool);

        if (data.AmountLeft >= capacity)
        {
            return false;
        }

        DeductReplenishCost(tool);

        data.AmountLeft++;
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

    internal void DeductReplenishCost(ToolItem tool)
    {
        int cost = ComputeReplenishCost(tool);
        ToolItem.ReplenishResources currencyType = tool.ReplenishResource;

        if (CurrencyIsDeductable(currencyType))
        {
            int currentAmount = CurrencyManager.GetCurrencyAmount((CurrencyType)currencyType);
            
            if (currentAmount - cost <= -0.5f)
            {
                return;
            }
        }

        if (cost > 0f && CurrencyIsDeductable(currencyType))
        {
            CurrencyManager.TakeCurrency(Mathf.RoundToInt(cost), (CurrencyType)currencyType, showCounter: true);
        }
    }
    private IEnumerator RegenLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(RegenIntervalSeconds);
            TryRegenRedTools();
        }
    }

    private void TryRegenRedTools()
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
            anyChanged |= ReplenishTool(tool);
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
