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
/// hides this mod from both local and server mod lists
/// </summary>
public static class ModDetectionBypass
{
    private const string ModFolderName = "ModProfileBypass";

    public static void Apply(Harmony harmony)
    {
        PatchLocalModList(harmony);
        PatchServerModList(harmony);
    }

    // -- local mod list patch --

    private static void PatchLocalModList(Harmony harmony)
    {
        var target = AccessTools.Method(typeof(ModManager), "GetModNameList")
                     ?? AccessTools.Method(typeof(ModManager), "GetGameplayRelevantModNameList");

        if (target == null)
        {
            ModEntry.Log.Error("ModManager mod list method not found, skip local bypass", 1);
            return;
        }

        harmony.Patch(
            target,
            postfix: new HarmonyMethod(typeof(ModDetectionBypass), nameof(RemoveFromList))
        );
    }

    [HarmonyPostfix]
    public static List<string> RemoveFromList(List<string> __result)
    {
        __result.RemoveAll(mod => mod.StartsWith(ModFolderName));
        return __result;
    }

    // -- server mod list patch (transpiler on JoinFlow.Begin async state machine) --

    private static void PatchServerModList(Harmony harmony)
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
            ModEntry.Log.Error("JoinFlow.Begin.MoveNext not found", 1);
            return;
        }

        harmony.Patch(
            moveNext,
            transpiler: new HarmonyMethod(typeof(ModDetectionBypass), nameof(TranspileMoveNext))
        );
    }

    public static void RemoveFromServerList(ref List<string> __result)
    {
        __result.RemoveAll(mod => mod.StartsWith(ModFolderName));
    }

    // look for the same IL pattern as heybox: stloc.s [local7] -> ldarg.0 -> ldflda -> ldfld
    private static IEnumerable<CodeInstruction> TranspileMoveNext(IEnumerable<CodeInstruction> instructions)
    {
        var list = new List<CodeInstruction>(instructions);
        int insertPoint = -1;

        for (int i = 0; i < list.Count; i++)
        {
            if (i + 3 >= list.Count) continue;
            if (list[i].opcode != OpCodes.Stloc_S) continue;
            if (list[i].operand is not LocalBuilder { LocalIndex: 7 }) continue;
            if (list[i + 1].opcode != OpCodes.Ldarg_0) continue;
            if (list[i + 2].opcode != OpCodes.Ldflda) continue;
            if (list[i + 3].opcode != OpCodes.Ldfld) continue;

            insertPoint = i + 9;
            break;
        }

        if (insertPoint > 0)
        {
            var method = AccessTools.Method(
                typeof(ModDetectionBypass),
                nameof(RemoveFromServerList),
                new[] { typeof(List<string>).MakeByRefType() }
            );
            list.Insert(insertPoint++, new CodeInstruction(OpCodes.Ldloca_S, (byte)8));
            list.Insert(insertPoint++, new CodeInstruction(OpCodes.Call, method));
        }

        return list.AsEnumerable();
    }
}
