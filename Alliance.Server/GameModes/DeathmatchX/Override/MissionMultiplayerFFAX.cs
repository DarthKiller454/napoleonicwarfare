using NetworkMessages.FromClient;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade
{
    public class MissionMultiplayerFFAX : MissionMultiplayerGameModeBase
    {
        public override bool IsGameModeHidingAllAgentVisuals
        {
            get
            {
                return true;
            }
        }
        public override bool IsGameModeUsingOpposingTeams
        {
            get
            {
                return false;
            }
        }

        public override MultiplayerGameType GetMissionType()
        {
            return MultiplayerGameType.Duel;
        }

        public override void AfterStart()
        {
            string strValue = MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            string strValue2 = MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            BasicCultureObject @object = MBObjectManager.Instance.GetObject<BasicCultureObject>(strValue);
            BasicCultureObject object2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(strValue2);
            MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(@object, object2);
            Banner banner = new Banner(@object.Banner, multiplayerBattleColors.AttackerColors.BannerBackgroundColorUint, multiplayerBattleColors.AttackerColors.BannerForegroundColorUint);
            Banner banner2 = new Banner(object2.Banner, multiplayerBattleColors.DefenderColors.BannerBackgroundColorUint, multiplayerBattleColors.DefenderColors.BannerForegroundColorUint);
            Team team = base.Mission.Teams.Add(BattleSideEnum.Attacker, multiplayerBattleColors.AttackerColors.BannerBackgroundColorUint, multiplayerBattleColors.AttackerColors.BannerForegroundColorUint, banner, true, false, true);
            Team team2 = base.Mission.Teams.Add(BattleSideEnum.Defender, multiplayerBattleColors.DefenderColors.BannerBackgroundColorUint, multiplayerBattleColors.DefenderColors.BannerForegroundColorUint, banner2, true, false, true);
            team.SetIsEnemyOf(team, true);
            team2.SetIsEnemyOf(team2, true);
            team.SetIsEnemyOf(team2, true);
            team2.SetIsEnemyOf(team, true);
        }

        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            networkPeer.AddComponent<FFAMissionRepresentative>();
        }

        protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
        {
        }

        public MissionMultiplayerFFAX()
        {
        }
    }
}
