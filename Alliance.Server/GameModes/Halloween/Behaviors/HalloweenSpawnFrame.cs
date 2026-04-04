using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.Halloween.Behaviors
{
    public class HalloweenSpawnFrame : SpawnFrameBehaviorBase
    {
        private List<GameEntity> _attackerSpawnPoints;
        private List<GameEntity> _defenderSpawnPoints;

        public override void Initialize()
        {
            base.Initialize();

            // Filter spawnpoints by tag just once
            _attackerSpawnPoints = SpawnPoints
                .Where(sp => sp.HasTag("attacker"))
                .ToList();

            _defenderSpawnPoints = SpawnPoints
                .Where(sp => sp.HasTag("defender"))
                .ToList();

            // If mapper forgot tags, fallback to all
            if (!_attackerSpawnPoints.Any() || !_defenderSpawnPoints.Any())
                InformationManager.DisplayMessage(
                    new InformationMessage("[SpawnFrame] Missing attacker/defender tags, using all spawnpoints.")
                );
        }

        public override MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn)
        {
            var list = team.Side == BattleSideEnum.Attacker
                ? _attackerSpawnPoints
                : _defenderSpawnPoints;

            // Fallback to all spawnpoints if list empty
            if (list == null || !list.Any())
                list = SpawnPoints.ToList();

            return GetBestSpawnPoint(list, hasMount);
        }

        private MatrixFrame GetBestSpawnPoint(List<GameEntity> spawnPoints, bool hasMount)
        {
            // Optionally filter out mount-restricted points first
            var validSpawns = hasMount
                ? spawnPoints.Where(sp => !sp.HasTag("exclude_mounted")).ToList()
                : spawnPoints;

            // Fallback if filtering removed all
            if (!validSpawns.Any())
                validSpawns = spawnPoints;

            // Pick a completely random spawnpoint
            var randomSpawn = validSpawns[MBRandom.RandomInt(validSpawns.Count)];
            var frame = randomSpawn.GetGlobalFrame();

            frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            return frame;
        }
    }
}