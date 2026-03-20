using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using ModProfileBypass.Patches;

namespace ModProfileBypass;

[ModInitializer(nameof(Initialize))]
public class ModEntry
{
    private const string HarmonyId = "com.paocai.mod.profilebypass";

    internal static readonly Logger Log = new Logger(HarmonyId, (LogType)0);

    private static readonly Harmony _harmony = new Harmony(HarmonyId);

    public static void Initialize()
    {
        // apply all [HarmonyPatch] attributed patches (ProfileDirPatch, etc.)
        _harmony.PatchAll(Assembly.GetExecutingAssembly());

        // manually wire up mod detection bypass since it needs dynamic method lookup
        ModDetectionBypass.Apply(_harmony);

        Log.Info("ModProfileBypass v1.0.0 initialized", 1);
    }
}
