using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Server.GameModes.Halloween.Behaviors
{
    public class HalloweenBotRandomizationBehavior : MissionBehavior
    {
        private readonly List<string> _sabreVariants = new()
        {
            "nwf_sabre_suisse_npc",
            "nwf_sabre_suisse_npc2",
            "nwf_sabre_suisse_npc3",
            "nwf_sabre_suisse_npc4"
        };

        private const string TargetTroopId = "mp_napoleonic_suisse_infantry_troop";

        // Track which agents we already randomized
        private readonly HashSet<Agent> _processedAgents = new();

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            // find any valid bots not yet processed
            foreach (var agent in Mission.Agents)
            {
                if (_processedAgents.Contains(agent))
                    continue;

                if (agent.Character == null || agent.Character.StringId != TargetTroopId)
                    continue;

                if (!agent.IsActive())
                    continue;

                RandomizeSabre(agent);
                _processedAgents.Add(agent);
            }
        }

        private void RandomizeSabre(Agent agent)
        {
            var randomId = _sabreVariants[MBRandom.RandomInt(_sabreVariants.Count)];
            var item = MBObjectManager.Instance.GetObject<ItemObject>(randomId);
            if (item == null)
                return;
            var missionWeapon = new MissionWeapon(item, null, null);
            agent.RemoveEquippedWeapon(EquipmentIndex.Weapon0);
            agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon0, ref missionWeapon);
            agent.WieldNextWeapon(HandIndex.MainHand);
        }
    }
}