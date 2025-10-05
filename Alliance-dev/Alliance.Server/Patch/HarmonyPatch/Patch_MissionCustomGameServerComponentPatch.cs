using Alliance.Common.Patch;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_MissionCustomGameServerComponentPatch
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MissionCustomGameServerComponentPatch));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // Get the original OnEndMission method from MissionCustomGameServerComponent
                var originalMethod = typeof(MissionCustomGameServerComponent).GetMethod("AddScoresToStats",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (originalMethod == null)
                {
                    // Handle the case where the method is not found
                    return false;
                }

                // Apply the patch to OnEndMission
                Harmony.Patch(originalMethod,
                    prefix: new HarmonyMethod(typeof(Patch_MissionCustomGameServerComponentPatch).GetMethod(nameof(AddScoresToStatsPrefix), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MissionCustomGameServerComponentPatch)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prefix method to skip the block of code that adds scores to stats for TeamDeathmatch and Siege missions. (and as a result crashes missions)
        /// </summary>
        public static bool AddScoresToStatsPrefix()
        {
            // Skips the original method entirely
            return false;
        }
    }
}