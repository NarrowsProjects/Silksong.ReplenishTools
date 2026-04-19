using BepInEx;
using BepInEx.Logging;
using Unity;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("com.NarrowsProjects.ReplenishTools", "Replenish tools over time", "1.0.0")]
public class ReplenishTools : BaseUnityPlugin
{
    public void Awake()
    {
        Logger.LogInfo("Loading ReplenishTools mod");
        Harmony.CreateAndPatchAll(typeof(ReplenishTools), null);
    }

    [HarmonyPatch(typeof(ToolItem), nameof(ToolItem.BaseStorageAmount), MethodType.Getter)]
    static class PatchToolCapacity
    {
        static void Postfix(ToolItem __instance, ref int __result)
        {
            if (__instance.Type == ToolItemType.Red)
                __result = Mathf.Max(1, __result / 2);
        }
    }
}
