using System.Collections.Generic;
using System.Linq;
using static Alliance.Common.Utilities.SceneList;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.Halloween
{
	public class HalloweenGameModeSettings : GameModeSettings
	{
		public HalloweenGameModeSettings() : base("Halloween", "Halloween", "Event mode.")
		{
		}

		public override void SetDefaultNativeOptions()
		{
			base.SetDefaultNativeOptions();
			TWOptions[OptionType.NumberOfBotsPerFormation] = 0;
            TWOptions[OptionType.NumberOfBotsTeam1] = 0;
			TWOptions[OptionType.NumberOfBotsTeam2] = 0;
			TWOptions[OptionType.MapTimeLimit] = 60;
            TWOptions[OptionType.MinScoreToWinMatch] = 1023000;
			TWOptions[OptionType.UnlimitedGold] = true;
            TWOptions[OptionType.DisableInactivityKick] = true;
            TWOptions[OptionType.SpectatorCamera] = 6;
        }

		public override void SetDefaultModOptions()
		{
			base.SetDefaultModOptions();
            ModOptions.ShowFlagMarkers = false;
            ModOptions.ShowScore = false;
			ModOptions.ShowOfficers = true;
			ModOptions.KillFeedEnabled = false;
        }

		public override List<SceneInfo> GetAvailableMaps()
		{
			return base.GetAvailableMaps();
		}

		public override List<OptionType> GetAvailableNativeOptions()
		{
			return new List<OptionType>
			{
				OptionType.CultureTeam1,
				OptionType.CultureTeam2,
                OptionType.NumberOfBotsTeam1,
				OptionType.NumberOfBotsTeam2,
                OptionType.MinScoreToWinMatch,
                OptionType.MapTimeLimit,
				OptionType.UseRealisticBlocking,
                OptionType.UnlimitedGold,
                OptionType.FriendlyFireDamageMeleeFriendPercent,
				OptionType.FriendlyFireDamageMeleeSelfPercent,
				OptionType.FriendlyFireDamageRangedFriendPercent,
				OptionType.FriendlyFireDamageRangedSelfPercent,
                OptionType.SpectatorCamera,
            };
		}

		public override List<string> GetAvailableModOptions()
		{
			return base.GetAvailableModOptions();
		}
	}
}