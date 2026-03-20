using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;

namespace ModProfileBypass.Patches;

/// <summary>
/// strips "modded/" prefix from profile dir so saves are shared with vanilla
/// </summary>
[HarmonyPatch(typeof(UserDataPathProvider), nameof(UserDataPathProvider.GetProfileDir))]
public static class ProfileDirPatch
{
    private const string ModdedPrefix = "modded/";

    [HarmonyPostfix]
    private static void Postfix(ref string __result)
    {
        if (__result.StartsWith(ModdedPrefix))
        {
            __result = __result.Substring(ModdedPrefix.Length);
        }
    }
}
