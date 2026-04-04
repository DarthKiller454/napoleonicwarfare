using Alliance.Common;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Patch.HarmonyPatch
{
    public class Patch_RangedSiegeWeapon_DetermineDefaultBattleSide
    {
        private static readonly Harmony Harmony =
            new Harmony(SubModule.ModuleId + nameof(Patch_RangedSiegeWeapon_DetermineDefaultBattleSide));

        private static bool _patched;

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                Harmony.Patch(
                    typeof(RangedSiegeWeapon).GetMethod(
                        "DetermineDefaultBattleSide",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_RangedSiegeWeapon_DetermineDefaultBattleSide)
                            .GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.Public))
                );
            }
            catch (Exception e)
            {
                Console.WriteLine("Alliance - ERROR in Patch_RangedSiegeWeapon_DetermineDefaultBattleSide");
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        public static bool Prefix(RangedSiegeWeapon __instance)
        {
            try
            {
                DestructableComponent destructableComponent =
                    __instance.GameEntity
                        .GetScriptComponents<DestructableComponent>()
                        .FirstOrDefault();

                if (destructableComponent != null)
                {
                    // Force neutral BEFORE native logic reads it
                    destructableComponent.BattleSide = BattleSideEnum.None;
                }
                else
                {
                    Console.WriteLine("Alliance - WARNING: DestructableComponent not found on siege weapon.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Alliance - ERROR in Prefix of Patch_RangedSiegeWeapon_DetermineDefaultBattleSide");
                Console.WriteLine(e);
            }

            // Allow original method to run with modified data
            return true;
        }
    }
}