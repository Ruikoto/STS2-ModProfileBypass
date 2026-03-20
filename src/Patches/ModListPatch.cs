using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace ModProfileBypass.Patches;

/// <summary>
/// hides this mod from the mod list so other players wont see it in multiplayer
/// </summary>
public static class ModListPatch
{
    private const string ModName = "ModProfileBypass";

    public static void Apply(Harmony harmony)
    {
        PatchGetModNameList(harmony);
        PatchJoinFlowServerList(harmony);
    }

    // --- client-side mod list ---

    private static void PatchGetModNameList(Harmony harmony)
    {
        var target = AccessTools.Method(typeof(ModManager), "GetModNameList")
                  ?? AccessTools.Method(typeof(ModManager), "GetGameplayRelevantModNameList");

        if (target == null)
        {
            ModEntry.Log.Error("ModManager mod list method not found, skip client bypass", 1);
            return;
        }

        harmony.Patch(
            target,
            postfix: new HarmonyMethod((Delegate)new Func<List<string>, List<string>>(RemoveFromList))
        );
        ModEntry.Log.Info("Client mod list bypass applied", 1);
    }

    private static List<string> RemoveFromList(List<string> __result)
    {
        __result.RemoveAll(m => m.StartsWith(ModName));
        return __result;
    }

    // --- server-side mod list (JoinFlow async state machine) ---

    private static void PatchJoinFlowServerList(Harmony harmony)
    {
        var beginMethod = typeof(JoinFlow).GetMethod("Begin", BindingFlags.Instance | BindingFlags.Public);
        if (beginMethod == null)
        {
            ModEntry.Log.Error("JoinFlow.Begin not found, skip server bypass", 1);
            return;
        }

        var asyncAttr = beginMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
        if (asyncAttr == null)
        {
            ModEntry.Log.Error("JoinFlow.Begin async state machine not found", 1);
            return;
        }

        var moveNext = asyncAttr.StateMachineType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        if (moveNext == null)
        {
            ModEntry.Log.Error("JoinFlow MoveNext not found", 1);
            return;
        }

        harmony.Patch(
            moveNext,
            transpiler: new HarmonyMethod((Delegate)new Func<IEnumerable<CodeInstruction>, IEnumerable<CodeInstruction>>(TranspileMoveNext))
        );
        ModEntry.Log.Info("Server mod list bypass applied", 1);
    }

    private static void RemoveFromServerList(ref List<string> __result)
    {
        __result.RemoveAll(m => m.StartsWith(ModName));
    }

    private static IEnumerable<CodeInstruction> TranspileMoveNext(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        int insertPoint = -1;

        // look for the same pattern as the original mod: Stloc_S with local index 7
        for (int i = 0; i < codes.Count; i++)
        {
            if (i + 3 >= codes.Count) continue;
            if (codes[i].opcode == OpCodes.Stloc_S
                && codes[i].operand is LocalBuilder lb && lb.LocalIndex == 7
                && codes[i + 1].opcode == OpCodes.Ldarg_0
                && codes[i + 2].opcode == OpCodes.Ldflda
                && codes[i + 3].opcode == OpCodes.Ldfld)
            {
                insertPoint = i + 9;
                ModEntry.Log.Info("Found server mod list patch point", 1);
                break;
            }
        }

        if (insertPoint > 0)
        {
            codes.Insert(insertPoint++, new CodeInstruction(OpCodes.Ldloca_S, (byte)8));
            codes.Insert(insertPoint, new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(ModListPatch), nameof(RemoveFromServerList),
                    new[] { typeof(List<string>).MakeByRefType() })));
            ModEntry.Log.Info("Server mod list transpiler injected", 1);
        }
        else
        {
            ModEntry.Log.Warn("Server mod list patch point not found, server bypass skipped", 1);
        }

        return codes.AsEnumerable();
    }
}
