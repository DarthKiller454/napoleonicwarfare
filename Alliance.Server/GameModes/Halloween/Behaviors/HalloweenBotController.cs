using Alliance.Common.Extensions.TroopSpawner.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Halloween.Behaviors
{
    public class WeightedTroop
    {
        public string TroopId;                  // Troop 's string ID
        public float BaseWeight;                // initial weight (% Chance) at spawn start
        public float WeightIncreasePerMinute;   // how much the weight increases/decreases per minute
        public float MaxWeight;                 // cap at 100%
    }
    public class HalloweenBotController : MissionBehavior
    {
        private const int GlobalHardCap = 900;               // absolute maximum combined players+bots
        private float spawnerTimer = 10f;                    // Initial Wait time until the first wave spawns
        private const int MaxSpawnBatch = 1;                 // spawn at most this many in a single tick loop
        private const float WaveActiveDurationMin = 1f;      // how long a wave stays active (seconds) minimum
        private const float WaveActiveDurationMax = 10f;     // max active duration
        private const float WaveInactiveDurationMin = 45f;   // how long between waves (seconds) min
        private const float WaveInactiveDurationMax = 120f;  // max inactive time between waves
        private const int MinDefenderBotsPerWave = 50;       // minimum bots to spawn per wave
        private float _chargeTimer = 0f;                     // timer to throttle charge commands
        private const float ChargeCooldown = 10f;            // seconds between charge orders
        private const float DefenderGrowthMultiplier = 4.0f; // how quickly defenders scale with attackers
        private const float DefenderGrowthExponent = 0.85f;  // exponent for scaling defenders
        private float _startTimer = 300f;                    // initial delay before first wave (currently 5 minutes)
        private const float AssistBlend = 0.75f;             // blending weight towards desired defenders from attacker count

        private bool _spawnerActive;
        private readonly List<Agent> _spawnedBots = new();

        private static readonly (int attackers, int defenders)[] DefenderAnchors = new (int, int)[]
        {
            (1, 50),
            (2, 50),
            (10, 60),
            (30, 100),
            (100, 300),
            (200, 500)
        }; 
        private readonly WeightedTroop[] _weightedTroops = new WeightedTroop[]
{
        new WeightedTroop
        {
            TroopId = "mp_napoleonic_suisse_infantry_troop",
            BaseWeight = 100f,
            WeightIncreasePerMinute = 0f,
            MaxWeight = 100f
        },
        new WeightedTroop
        {
            TroopId = "mp_napoleonic_suisse_heavy_troop",
            BaseWeight = 0f,              // 0% for first 5 minutes
            WeightIncreasePerMinute = 5f, // 5% per minute after 5 minutes
            MaxWeight = 100f
        }
};

        // internal state
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();


            _spawnerActive = false;
        }
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            CleanupSpawnedList();

            _startTimer -= dt;
            spawnerTimer -= dt;

            if (_startTimer > 0f)
                return;

            if (_spawnerActive)
            {
                TryMaintainDefenderBots();

                if (spawnerTimer <= 0f)
                    EnterInactivePhase();
            }
            else
            {
                int totalAlive = CountAllAliveEntities();
                if (totalAlive < MinDefenderBotsPerWave || spawnerTimer <= 0f)
                {
                    EnterActivePhase();
                }
            }
            _chargeTimer -= dt;
            if (_chargeTimer <= 0f && _spawnedBots.Count > 0)
            {
                // just take the first agent
                Agent first = _spawnedBots[0];
                Formation formation = first.Formation;
                if (formation != null)
                {
                    formation.SetControlledByAI(true);
                    formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                }

                _chargeTimer = ChargeCooldown;
            }
        }
        private int ComputeTargetDefenders(int aliveAttackers)
        {
            if (aliveAttackers <= 0)
                return MinDefenderBotsPerWave;

            float baseMin = MinDefenderBotsPerWave;
            float baseVal = baseMin + DefenderGrowthMultiplier * (float)Math.Pow(aliveAttackers, DefenderGrowthExponent);

            float anchorVal = InterpolateAnchors(aliveAttackers);

            float blended = (1f - AssistBlend) * baseVal + AssistBlend * anchorVal;

            int target = (int)Math.Round(Math.Min(Math.Max(blended, MinDefenderBotsPerWave), GlobalHardCap));
            return target;
        }

        private float InterpolateAnchors(int n)
        {
            if (n <= DefenderAnchors[0].attackers)
                return DefenderAnchors[0].defenders;

            var last = DefenderAnchors[DefenderAnchors.Length - 1];
            if (n >= last.attackers)
            {
                var prev = DefenderAnchors[DefenderAnchors.Length - 2];
                float slope = (float)(last.defenders - prev.defenders) / (last.attackers - prev.attackers);
                return last.defenders + slope * (n - last.attackers);
            }

            for (int i = 0; i < DefenderAnchors.Length - 1; ++i)
            {
                var a = DefenderAnchors[i];
                var b = DefenderAnchors[i + 1];
                if (n >= a.attackers && n <= b.attackers)
                {
                    if (a.attackers == b.attackers) return a.defenders;
                    float t = (n - a.attackers) / (float)(b.attackers - a.attackers);
                    return a.defenders + t * (b.defenders - a.defenders);
                }
            }

            return last.defenders;
        }
        private string ChooseWeightedTroop()
        {
            float elapsedMinutes = Mission.CurrentTime / 60f; // in minutes

            float heavyWeight = 0f;
            float totalWeight = 0f;


            // compute heavy's dynamic weight
            var heavyTroop = _weightedTroops.First(t => t.TroopId == "mp_napoleonic_suisse_heavy_troop");
            if (elapsedMinutes > 10f) // after 10 minutes, since we have to account for the initial 5 minute delay. TODO: Sync with _startTimer?
            {
                heavyWeight = Math.Min(heavyTroop.BaseWeight + heavyTroop.WeightIncreasePerMinute * (elapsedMinutes - 5f), heavyTroop.MaxWeight);
            }
            else
            {
                heavyWeight = heavyTroop.BaseWeight; // should be 0 for first 5 minutes
            }

            // infantry weight declines as heavy rises
            var infantryTroop = _weightedTroops.First(t => t.TroopId == "mp_napoleonic_suisse_infantry_troop");
            float infantryWeight = Math.Max(infantryTroop.BaseWeight - heavyWeight, 0f);

            totalWeight = infantryWeight + heavyWeight;

            float roll = MBRandom.RandomFloatRanged(0f, totalWeight);

            if (roll <= infantryWeight)
                return infantryTroop.TroopId;
            else
                return heavyTroop.TroopId;
        }
        private void TryMaintainDefenderBots()
{
    try
    {
        // Count alive agents in both teams
        int aliveAttackers = 0;
        int aliveDefenders = 0;
        List<Agent> agents = Mission.Current.Agents.ToList();

        foreach (Agent agent in agents)
        {
            if (agent != null && agent.IsActive())
            {
                if (agent.Team == Mission.AttackerTeam)
                    aliveAttackers++;
                else if (agent.Team == Mission.DefenderTeam)
                    aliveDefenders++;
            }
        }
        CleanupSpawnedList();
        int spawnedBotsCount = _spawnedBots.Count;

        int maxAllowedDefenders = ComputeTargetDefenders(aliveAttackers);

        // Compute how many more we can spawn right now
        int toSpawn = maxAllowedDefenders - aliveDefenders;
        if (toSpawn <= 0)
            return;

        // Respect the global hard cap
        int globalAlive = CountAllAliveEntities();
        int allowedGlobalSpawn = Math.Max(0, GlobalHardCap - globalAlive);
        toSpawn = Math.Min(toSpawn, allowedGlobalSpawn);

        int batch = Math.Min(toSpawn, MaxSpawnBatch);
        for (int i = 0; i < batch; ++i)
            SpawnOneBotForTeam(Mission.DefenderTeam);
    }
    catch (Exception e)
    {
        InformationManager.DisplayMessage(new InformationMessage($"HalloweenBotController spawn error: {e.Message}"));
    }
}

        private void SpawnOneBotForTeam(Team team)
        {
            var chosenTroopId = ChooseWeightedTroop();
            BasicCharacterObject character = MBObjectManager.Instance.GetObject<BasicCharacterObject>(chosenTroopId);
            if (character == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"[HalloweenBotController] Missing troop: {chosenTroopId}"));
                return;
            }

            BasicCultureObject cultureDef = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));

            bool success = SpawnHelper.SpawnBot(
                out Agent agent,
                Mission.DefenderTeam,
                cultureDef,
                character,
                botDifficulty: 1f,
                mortalityState: Agent.MortalityState.Mortal);

            if (!success || agent == null)
                return;

            _spawnedBots.Add(agent);
            agent.SetIsAIPaused(false);
            agent.AIStateFlags |= Agent.AIStateFlag.Alarmed;

            Formation formation = agent.Formation;
            if (formation != null)
            {
                formation.SetControlledByAI(true);
                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }
        }

        private void CleanupSpawnedList()
        {
            for (int i = _spawnedBots.Count - 1; i >= 0; --i)
            {
                Agent a = _spawnedBots[i];
                if (a == null || !a.IsActive())
                    _spawnedBots.RemoveAt(i);
            }
        }

        private int CountAllAliveEntities()
        {
            int activeAgents = 0;
            List<Agent> agents = Mission.Current.Agents.ToList();

            foreach (Agent agent in agents)
            {
                if (agent != null && agent.IsActive())
                {
                    activeAgents++;
                }
            }
            return activeAgents;
        }

        private void EnterActivePhase()
        {
            _spawnerActive = true;
            spawnerTimer = MBRandom.RandomFloatRanged(WaveActiveDurationMin, WaveActiveDurationMax);

            TryMaintainDefenderBots();
        }

        private void EnterInactivePhase()
        {
            _spawnerActive = false;
            spawnerTimer = MBRandom.RandomFloatRanged(WaveInactiveDurationMin, WaveInactiveDurationMax);
        }
    }

}