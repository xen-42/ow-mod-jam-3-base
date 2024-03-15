using HarmonyLib;
using NewHorizons.Utility.DebugTools;

namespace ModJam3;

[HarmonyPatch(typeof(DebugReload))]
internal class NewHorizonsPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("ReloadConfigs")]
    public static void DebugReload_ReloadConfigs() => ModJam3.Instance.FixCompatIssues();
}
