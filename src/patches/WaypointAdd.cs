using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

[HarmonyPatch(typeof(GuiDialogAddWayPoint), "onSave")]
class Patch_GuiDialogAddWayPoint_onSave
{
    public static readonly MethodInfo toReplaceWith = AccessTools.Method(typeof(Patch_GuiDialogAddWayPoint_onSave), nameof(BroadcastWaypoint));
    public static void BroadcastWaypoint(ICoreClientAPI capi, string message, GuiDialogEditWayPoint instance)
    {
        if (capi != null)
        {
            if (instance.SingleComposer.GetSwitch(Settings.ShouldShareSwitchName).On)
            {
                WaypointTogetherContinued.Core mod = capi.ModLoader.GetModSystem<WaypointTogetherContinued.Core>();
                var waypoint = Traverse.Create(instance).Field("waypoint").GetValue<Waypoint>();
                mod.client?.network.ShareWaypoint(message, waypoint);
                string messageToTheUser = Lang.Get("waypointtogethercontinued:waypoint-shared");
                capi.ShowChatMessage(messageToTheUser);
            }
            capi.SendChatMessage(message);
        }
    }
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var cloned = instructions.ToList();
        var found = 0;
        for (int i = 0; i < cloned.Count; i++)
        {
            var instruction = cloned[i];
            CodeInstruction nextInstruction = null;
            if (i < cloned.Count - 1) {
                nextInstruction = cloned[i + 1];
            }
            if (found == 0 && instruction.opcode == OpCodes.Ldnull && nextInstruction?.opcode == OpCodes.Callvirt && nextInstruction.operand is MethodInfo method && method == Settings.SendChatMessageMethod)
            {
                found = 1;
            }
            else if (found == 1)
            {
                // ORIGINAL: list.RemoveAt(list.Count - 1); // remove 'ldnull'
                yield return new CodeInstruction(OpCodes.Ldarg_0); // load 'this'
                yield return new CodeInstruction(OpCodes.Call, toReplaceWith);
                found = 2;
            }
            else
            {
                yield return instruction;
            }
        }

        if (found != 2)
        {
            throw new ArgumentException("Cannot find `ldnull` before `callvirt` in GuiDialogAddWayPoint.onSave");
        }
    }
}
