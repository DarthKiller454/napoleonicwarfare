using System.Collections.Generic;
using System.Linq;
using static Alliance.Common.Utilities.SceneList;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.Groupfight
{
	public class GroupfightGameModeSettings : GameModeSettings
	{
		public GroupfightGameModeSettings() : base("Groupfight", "Groupfight", "Groupfight mode.")
		{
		}

		public override void SetDefaultNativeOptions()
		{
			base.SetDefaultNativeOptions();
			TWOptions[OptionType.NumberOfBotsPerFormation] = 0;
			TWOptions[OptionType.UnlimitedGold] = true;
			TWOptions[OptionType.RoundTotal] = 15;
			TWOptions[OptionType.RoundPreparationTimeLimit] = 30;
			TWOptions[OptionType.RoundTimeLimit] = 3600;
            TWOptions[OptionType.WarmupTimeLimitInSeconds] = 60;
            TWOptions[OptionType.FriendlyFireDamageMeleeFriendPercent] = 100;
            TWOptions[OptionType.FriendlyFireDamageMeleeSelfPercent] = 0;
            TWOptions[OptionType.FriendlyFireDamageRangedFriendPercent] = 100;
            TWOptions[OptionType.FriendlyFireDamageRangedSelfPercent] = 0;
        }

		public override void SetDefaultModOptions()
		{
			base.SetDefaultModOptions();
			ModOptions.EnableFormation = false;
			ModOptions.TimeBeforeFlagRemoval = 1;
			ModOptions.MoraleMultiplierForFlag = 0f;
			ModOptions.MoraleMultiplierForLastFlag = 0f;
			ModOptions.AllowSpawnInRound = true;
			ModOptions.ShowFlagMarkers = true;
			ModOptions.ShowScore = true;
			ModOptions.ShowOfficers = true;
		}

		public override List<SceneInfo> GetAvailableMaps()
		{
			return base.GetAvailableMaps().Where(scene => scene.HasSpawnForAttacker && scene.HasSpawnForDefender && scene.HasSpawnVisual).ToList();
		}

		public override List<OptionType> GetAvailableNativeOptions()
		{
			return new List<OptionType>
			{
				OptionType.GamePassword,
                OptionType.CultureTeam1,
				OptionType.CultureTeam2,
				OptionType.NumberOfBotsTeam1,
				OptionType.NumberOfBotsTeam2,
				OptionType.RoundPreparationTimeLimit,
				OptionType.RoundTimeLimit,
				OptionType.RoundTotal,
				OptionType.WarmupTimeLimitInSeconds,
				OptionType.UnlimitedGold,
				OptionType.AutoTeamBalanceThreshold,
				OptionType.FriendlyFireDamageMeleeFriendPercent,
				OptionType.FriendlyFireDamageMeleeSelfPercent,
				OptionType.FriendlyFireDamageRangedFriendPercent,
				OptionType.FriendlyFireDamageRangedSelfPercent,
                OptionType.DisableInactivityKick,
                OptionType.UseRealisticBlocking,
				OptionType.GoldGainChangePercentageTeam1,
                OptionType.GoldGainChangePercentageTeam2,
				OptionType.AllowIndividualBanners,
				OptionType.WelcomeMessage,
            };
		}

		public override List<string> GetAvailableModOptions()
		{
			return base.GetAvailableModOptions();
		}
	}
}