using HarmonyLib;
using System;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
    class Patch_MultiplayerIntermissionVotingManager
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MultiplayerIntermissionVotingManager));
        private static bool _patched;

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                var originalMethod = typeof(MultiplayerIntermissionVotingManager)
                    .GetMethod("SelectRandomCultures", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                if (originalMethod == null)
                {
                    Log("SelectRandomCultures method not found!", LogLevel.Error);
                    return false;
                }

                var prefixMethod = typeof(Patch_MultiplayerIntermissionVotingManager)
                    .GetMethod(nameof(Prefix_SelectRandomCultures), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

                Harmony.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MultiplayerIntermissionVotingManager)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        public static readonly string[] CustomCultures = new[]
        {
            "nwf_austria",
            "nwf_france",
            "nwf_britain",
            "nwf_prussia"
        };

        private static bool Prefix_SelectRandomCultures(MultiplayerOptions.MultiplayerOptionsAccessMode accessMode)
        {
            try
            {
                var random = new Random();
                string value1 = CustomCultures[random.Next(CustomCultures.Length)];
                string value2 = CustomCultures[random.Next(CustomCultures.Length)];

                MultiplayerOptions.OptionType.CultureTeam1.SetValue(value1, accessMode);
                MultiplayerOptions.OptionType.CultureTeam2.SetValue(value2, accessMode);

                Log($"Replaced random cultures with: {value1} vs {value2}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Log($"Error in Prefix_SelectRandomCultures: {ex}", LogLevel.Error);
            }

            return false;
        }
    }
}