using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
    class Patch_WeakGameEntity
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_WeakGameEntity));

        private static bool _patched;

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                var originalMethod = typeof(WeakGameEntity).GetMethod(
                    nameof(WeakGameEntity.HasTag),
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new Type[] { typeof(string) },
                    null);

                if (originalMethod == null)
                    return false;

                Harmony.Patch(
                    originalMethod,
                    prefix: new HarmonyMethod(
                        typeof(Patch_WeakGameEntity).GetMethod(
                            nameof(Prefix_HasTag),
                            BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_WeakGameEntity)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prevent crashes when WeakGameEntity becomes invalid before HasTag() native call.
        /// </summary>
        public static bool Prefix_HasTag(ref bool __result, WeakGameEntity __instance, string tag)
        {
            try
            {
                if (__instance.Equals(WeakGameEntity.Invalid))
                {
                    __result = false;
                    return false;
                }

                if (string.IsNullOrEmpty(tag))
                {
                    __result = false;
                    return false;
                }

                return true; // continue to original method
            }
            catch
            {
                __result = false;
                return false;
            }
        }
    }
}