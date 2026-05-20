using Alliance.Client.Extensions.AgentsCount.ViewModels;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;

namespace Alliance.Client.Extensions.AgentsCount.Views
{
    public class AgentsCountView : MissionView
    {
        private GauntletLayer _layer;
        private AgentsCountVM _dataSource;

        public AgentsCountView()
        {
        }

        public override void EarlyStart()
        {
            ViewOrderPriority = 20;
            _dataSource = new AgentsCountVM();
            _layer = new GauntletLayer("GauntletLayer", ViewOrderPriority, false);
            _layer.LoadMovie("AgentsCountHUD", _dataSource);
            MissionScreen.AddLayer(_layer);
        }

        public override void OnMissionScreenFinalize()
        {
            MissionScreen.RemoveLayer(_layer);
            _layer = null;
            _dataSource?.OnFinalize();
            _dataSource = null;
        }

        // Calculated by client
        public void RefreshAgentsAlive()
        {
            Mission mission = Mission.Current;
            if (mission == null)
                return;

            Team attackerTeam = mission.AttackerTeam;
            Team defenderTeam = mission.DefenderTeam;
            Team enemyTeam = mission.AttackerTeam;

            Team myTeam = GameNetwork.MyPeer.GetComponent<MissionPeer>().Team;

            if (myTeam != null && myTeam.IsAttacker)
            {
                enemyTeam = defenderTeam;
            }
            else
            {
                enemyTeam = attackerTeam;
            }

            _dataSource.AgentsAlive = myTeam?.ActiveAgents?.Count ?? 0;
            _dataSource.AgentsDead = enemyTeam?.ActiveAgents?.Count ?? 0;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            RefreshAgentsAlive();
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            RefreshAgentsAlive();
        }

        public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
        {
            RefreshAgentsAlive();
        }


        public override void OnMissionScreenTick(float dt)
        {
            if (Input.IsGameKeyDown(5))
            {
                _dataSource.IsVisible = true;
            }
            else
            {
                _dataSource.IsVisible = false;
            }
        }
    }
}