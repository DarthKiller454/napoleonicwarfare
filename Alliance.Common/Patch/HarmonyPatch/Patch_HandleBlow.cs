using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using Alliance.Common;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace Alliance.Client.Patch.HarmonyPatch
{
    public class Patch_DestructableComponent_OnHit
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_DestructableComponent_OnHit));
        private static bool _patched;

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                // IMPORTANT: Patch the correct overload with explicit signature matching
                Harmony.Patch(
                    typeof(DestructableComponent).GetMethod(
                        "OnHit",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                        null,
                        new Type[]
                        {
                            typeof(Agent),
                            typeof(int),
                            typeof(Vec3),
                            typeof(Vec3),
                            typeof(MissionWeapon).MakeByRefType(), // "in" parameter
                            typeof(int),
                            typeof(ScriptComponentBehavior),
                            typeof(bool).MakeByRefType(),  // out
                            typeof(float).MakeByRefType()  // out
                        },
                        null
                    ),
                    prefix: new HarmonyMethod(typeof(Patch_DestructableComponent_OnHit)
                        .GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.Public))
                );
            }
            catch (Exception e)
            {
                Console.WriteLine("Alliance - ERROR in Patch_DestructableComponent_OnHit");
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        public static bool Prefix(
            Agent attackerAgent,
            ref int inflictedDamage,
            Vec3 impactPosition,
            Vec3 impactDirection,
            in MissionWeapon weapon,
            int affectorWeaponSlotOrMissileIndex,
            ScriptComponentBehavior attackerScriptComponentBehavior,
            ref bool reportDamage,
            ref float modifiedDamage)
        {
            reportDamage = false;
            modifiedDamage = (float)inflictedDamage;

            WeaponComponentData weaponData = weapon.CurrentUsageItem;
            if (weaponData == null || attackerAgent == null || !attackerAgent.ActionSet.IsValid || !attackerAgent.IsActive())
            return true;

            ActionIndexCache currentAction = attackerAgent.GetCurrentAction(1);
            if (currentAction == null
            || currentAction == ActionIndexCache.act_none
            || currentAction.Index < 0
            || string.IsNullOrEmpty(currentAction.GetName())
            || currentAction.GetName == ActionIndexCache.act_none.GetName)
            {
                return true;
            }
            string actionName = null;
            try
            {
                actionName = attackerAgent.ActionSet.GetAnimationName(currentAction);
            }
            catch (AccessViolationException)
            {
                return true;
            }

            if (actionName.Contains("overswing") || actionName.Contains("thrust"))
            {
                if (weaponData.IsMeleeWeapon && weaponData.ThrustDamage > 0 && weaponData.ThrustDamageType == DamageTypes.Pierce)
                {
                    int newDamage = Math.Max(weaponData.ThrustDamage, weaponData.SwingDamage) / 5;
                    float progress = attackerAgent.GetCurrentActionProgress(1);

                    if (progress < 0.2f)
                    {
                        inflictedDamage = 0;
                        return false;
                    }
                    else
                    {
                        float scaling = 1f - 4f * (progress - 0.5f) * (progress - 0.5f);
                        scaling = TaleWorlds.Library.MathF.Max(0f, scaling);
                        inflictedDamage = (int)(newDamage * scaling);
                    }

                    reportDamage = inflictedDamage > 0;
                    modifiedDamage = inflictedDamage;
                }
            }
            return true;
        }
    }
}