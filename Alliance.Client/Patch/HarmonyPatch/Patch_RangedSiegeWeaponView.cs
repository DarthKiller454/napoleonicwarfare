using Alliance.Common.Extensions.Artillery;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.SiegeWeapon;
using Logger = Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
    public static class Patch_RangedSiegeWeaponView_HandleUserAiming
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_RangedSiegeWeaponView_HandleUserAiming));

        private static bool _patched;

        private static readonly FieldInfo UsesMouseForAimingField =
            AccessTools.Field(typeof(RangedSiegeWeaponView), "UsesMouseForAiming");

        public static bool Patch()
        {
            if (_patched)
                return false;
            _patched = true;

            MethodInfo target = AccessTools.Method(
                typeof(RangedSiegeWeaponView),
                "HandleUserAiming",
                new Type[]
                {
                    typeof(float)
                });

            MethodInfo prefix = AccessTools.Method(
                typeof(Patch_RangedSiegeWeaponView_HandleUserAiming),
                nameof(Prefix));


            Harmony.Patch(
                target,
                prefix: new HarmonyMethod(prefix));
            return true;
        }

        private static bool Prefix(RangedSiegeWeaponView __instance, float dt)
        {
            bool shouldGiveInput = false;
            float inputX = 0f;
            float inputY = 0f;

            if (__instance.PilotAgent != null &&
                (__instance.PilotAgent.IsMainAgent || __instance.RangedSiegeWeapon.PlayerForceUse))
            {
                bool usesMouseForAiming = false;

                if (UsesMouseForAimingField != null)
                {
                    usesMouseForAiming = (bool)UsesMouseForAimingField.GetValue(__instance);
                }

                if (usesMouseForAiming)
                {
                    InputContext input = __instance.MissionScreen.SceneLayer.Input;

                    float mouseScale = dt * 1666.6666f;
                    float mouseX = input.GetMouseMoveX() + mouseScale * input.GetGameKeyAxis("CameraAxisX");
                    float mouseY = input.GetMouseMoveY() + -mouseScale * input.GetGameKeyAxis("CameraAxisY");

                    if (NativeConfig.InvertMouse)
                    {
                        mouseY *= -1f;
                    }

                    Vec2 aimVector = new Vec2(-mouseX, -mouseY);

                    if (aimVector.IsNonZero())
                    {
                        float aimLength = aimVector.Normalize();
                        aimLength = MathF.Min(5f, MathF.Pow(aimLength, 1.5f) * 0.025f);
                        aimVector *= aimLength;

                        inputX = aimVector.x;
                        inputY = aimVector.y;
                    }
                }
                else
                {
                    InputContext input = __instance.MissionScreen.SceneLayer.Input;


                    if (MoveableControlInput.IsDown(MoveableControlAction.TurnLeft))
                    {
                        inputX = 1f;
                    }
                    else if (MoveableControlInput.IsDown(MoveableControlAction.TurnRight))
                    {
                        inputX = -1f;
                    }

                    if (MoveableControlInput.IsDown(MoveableControlAction.MoveForward))
                    {
                        inputY = 1f;
                    }
                    else if (MoveableControlInput.IsDown(MoveableControlAction.MoveBackward))
                    {
                        inputY = -1f;
                    }
                }

                if (inputX != 0f)
                {
                    shouldGiveInput = true;
                }

                if (inputY != 0f)
                {
                    shouldGiveInput = true;
                }
            }

            if (shouldGiveInput)
            {
                __instance.RangedSiegeWeapon.GiveInput(inputX, inputY);
            }

            return false;
        }
    }
}