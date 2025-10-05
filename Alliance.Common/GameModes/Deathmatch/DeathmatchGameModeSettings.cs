using System.Collections.Generic;
using System.Linq;
using static Alliance.Common.Utilities.SceneList;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.Deathmatch
{
	public class DeathmatchGameModeSettings : GameModeSettings
	{
		public DeathmatchGameModeSettings() : base("DeathmatchX", "Deathmatch", "DM mode.")
		{
		}

		public override void SetDefaultNativeOptions()
		{
			base.SetDefaultNativeOptions();
			TWOptions[OptionType.NumberOfBotsPerFormation] = 0;
            TWOptions[OptionType.NumberOfBotsTeam1] = 0;
			TWOptions[OptionType.NumberOfBotsTeam2] = 0;
            TWOptions[OptionType.MapTimeLimit] = 60;
            TWOptions[OptionType.UnlimitedGold] = true;
        }

		public override void SetDefaultModOptions()
		{
			base.SetDefaultModOptions();
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
                OptionType.MapTimeLimit,
                OptionType.UnlimitedGold
			};
		}

		public override List<string> GetAvailableModOptions()
		{
			return base.GetAvailableModOptions();
		}
	}
}