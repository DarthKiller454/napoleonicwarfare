using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using Alliance.Common;

namespace Alliance.Common.Patch.HarmonyPatch
{
    public class Patch_UsableMachine_OnInit
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_UsableMachine_OnInit));

        private static bool _patched;

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                Harmony.Patch(
                    typeof(UsableMachine).GetMethod("OnInit", BindingFlags.Instance | BindingFlags.NonPublic),
                    postfix: new HarmonyMethod(typeof(Patch_UsableMachine_OnInit).GetMethod(
                        nameof(Postfix), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine("Alliance - ERROR in Patch_UsableMachine_OnInit");
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        public static void Postfix(UsableMachine __instance)
        {
            try
            {
                if (__instance?.GameEntity?.HasTag("placedbyscript") != true)
                    return;

                Type usableMachineType = typeof(UsableMachine);

                FieldInfo attackerField = usableMachineType.GetField("_isDisabledForAttackerAIDueToEnemyInRange", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo defenderField = usableMachineType.GetField("_isDisabledForDefenderAIDueToEnemyInRange", BindingFlags.NonPublic | BindingFlags.Instance);

                if (attackerField != null)
                {
                    attackerField.SetValue(__instance, new QueryData<bool>(() => false, 5f));
                }

                if (defenderField != null)
                {
                    defenderField.SetValue(__instance, new QueryData<bool>(() => false, 5f));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Alliance - ERROR in Postfix of Patch_UsableMachine_OnInit");
                Console.WriteLine(e);
            }
        }
    }
}