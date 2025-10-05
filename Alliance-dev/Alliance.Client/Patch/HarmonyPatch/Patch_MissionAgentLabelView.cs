using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.Patch.HarmonyPatch
{
    class Patch_MissionAgentLabelView
    {
        private static readonly Harmony Harmony = new Harmony("Alliance.Patch.MissionAgentLabelView");
        private static bool _patched;

        public static bool Patch()
        {
            if (_patched)
                return false;

            try
            {
                MethodInfo targetMethod = typeof(MissionAgentLabelView).GetMethod("InitAgentLabel", BindingFlags.Instance | BindingFlags.NonPublic);
                Harmony.Patch(targetMethod, transpiler: new HarmonyMethod(typeof(Patch_MissionAgentLabelView).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic)));
                _patched = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to patch InitAgentLabel: {ex}");
                return false;
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            for (int i = 0; i < code.Count - 5; i++)
            {
                if (code[i].opcode == OpCodes.Ldc_R4 && (float)code[i].operand == 0.5f &&
                    code[i + 1].opcode == OpCodes.Ldc_R4 && (float)code[i + 1].operand == 0.5f &&
                    code[i + 2].opcode == OpCodes.Ldc_R4 && (float)code[i + 2].operand == 0.25f &&
                    code[i + 3].opcode == OpCodes.Ldc_R4 && (float)code[i + 3].operand == 0.25f &&
                    code[i + 4].opcode == OpCodes.Callvirt &&
                    code[i + 4].operand is MethodInfo method &&
                    method.Name == "SetVectorArgument")
                {
                    code[i] = new CodeInstruction(OpCodes.Ldc_R4, 0.4f);  // X offset
                    code[i + 1] = new CodeInstruction(OpCodes.Ldc_R4, 0.4f);  // Y offset
                    code[i + 2] = new CodeInstruction(OpCodes.Ldc_R4, 0.3f);  // Width
                    code[i + 3] = new CodeInstruction(OpCodes.Ldc_R4, 0.3f);  // Height
                    break;
                }
            }

            return code;
        }
    }
}