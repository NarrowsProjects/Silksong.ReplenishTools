using BepInEx;
using BepInEx.Logging;
using Unity;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("com.NarrowsProjects.ReplenishTools", "Replenish tools over time", "1.0.0")]
public class ReplenishTools : BaseUnityPlugin
{
    internal static bool IsRedTool(ToolItem tool) => tool.Type == ToolItemType.Red;
    internal static bool IsPercentageReplenish(ToolItem tool) => tool.ReplenishUsage == ToolItem.ReplenishUsages.Percentage;

    public void Awake()
    {
        Logger.LogInfo("Loading ReplenishTools mod");
        Harmony harmony = new Harmony("com.NarrowsProjects.ReplenishTools");
        harmony.PatchAll();
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
            if (IsRedTool(__instance) && IsPercentageReplenish(__instance)) {
                __result *= 0.5f;
            }
        }
    }
}
