using Alliance.Common.Patch;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_MissionCustomGameServerComponent
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MissionCustomGameServerComponent));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // Get the original OnEndMission method from MissionCustomGameServerComponent
                var originalMethod = typeof(MissionCustomGameServerComponent).GetMethod("OnEndMission",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (originalMethod == null)
                {
                    // Handle the case where the method is not found
                    return false;
                }

                // Apply the patch to OnEndMission
                Harmony.Patch(originalMethod,
                    prefix: new HarmonyMethod(typeof(Patch_MissionCustomGameServerComponent).GetMethod(nameof(OnEndMissionPatch), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MissionCustomGameServerComponent)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prefix method to skip the block of code that adds scores to stats for TeamDeathmatch and Siege missions.
        /// </summary>
        public static bool OnEndMissionPatch(MissionCustomGameServerComponent __instance)
        {
            // Use reflection to get the value of the _gameMode field
            FieldInfo gameModeField = typeof(MissionCustomGameServerComponent).GetField("_gameMode", BindingFlags.Instance | BindingFlags.NonPublic);
            if (gameModeField == null)
            {
                Log("GameMode field not found.", LogLevel.Error);
                return true;  // Proceed with method execution if field is not found
            }

            var gameMode = gameModeField.GetValue(__instance);
            if (gameMode == null)
            {
                Log("GameMode is null.", LogLevel.Error);
                return true;  // Proceed with method execution if gameMode is null
            }

            // Use reflection to call GetMissionType and explicitly cast the result
            var result = gameMode.GetType().GetMethod("GetMissionType").Invoke(gameMode, null);
            MultiplayerGameType missionType = (MultiplayerGameType)result;

            // Only skip the block that adds scores to stats for TeamDeathmatch or Siege
            if (missionType == MultiplayerGameType.Siege || missionType == MultiplayerGameType.TeamDeathmatch)
            {
                // Return false to skip the block below inside OnEndMission
                return false;
            }

            // Allow the execution to continue normally for other parts of OnEndMission
            return true;
        }
    }
}