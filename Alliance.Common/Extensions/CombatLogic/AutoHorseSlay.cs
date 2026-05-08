using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using NetworkMessages.FromServer;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace Alliance.Common.Extensions.CombatLogic
{
    public class AutoHorseSlay : MissionLogic
    {
        private readonly Dictionary<Agent, float> _riderlessTimers = new Dictionary<Agent, float>();

        private const float DespawnTime = 180f; // 3 minutes

        public AutoHorseSlay()
        {
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (!GameNetwork.IsServer)
                return;

            if (Mission.Current == null)
                return;
            //if (!Config.Instance.AutoHorseSlay)
            //  return;

            AgentReadOnlyList agents = Mission.Current.Agents;

            for (int i = 0; i < agents.Count; i++)
            {
                Agent agent = agents[i];

                if (agent == null || !agent.IsActive())
                    continue;

                if (!agent.IsMount)
                    continue;

                // Ignore dead horses
                if (agent.Health <= 0f)
                {
                    _riderlessTimers.Remove(agent);
                    continue;
                }

                Agent rider = agent.RiderAgent;

                // Horse has rider -> reset timer
                if (rider != null && rider.IsActive())
                {
                    _riderlessTimers.Remove(agent);
                    continue;
                }

                // No rider -> start / continue timer
                if (!_riderlessTimers.ContainsKey(agent))
                    _riderlessTimers[agent] = 0f;

                _riderlessTimers[agent] += dt;

                // Kill horse after 2 minutes abandoned
                if (_riderlessTimers[agent] >= DespawnTime)
                {
                    agent.DealDamage(agent, 2000);

                    _riderlessTimers.Remove(agent);
                }
            }
        }
    }
}