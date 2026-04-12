using BepInEx;
using BepInEx.Logging;
using Unity;
using HarmonyLib;

[BepInPlugin("com.NarrowsProjects.ReplenishTools", "Replenish tools over time", "1.0.0")]
public class ReplenishTools : BaseUnityPlugin
{
    public void Awake()
    {
        Logger.LogInfo("Loading ReplenishTools mod");
        Harmony.CreateAndPatchAll(typeof(ReplenishTools), null);
    }
}
