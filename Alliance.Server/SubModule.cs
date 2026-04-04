using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.ExtendedXML;
using Alliance.Common.Core.Security;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.CombatLogic;
using Alliance.Common.Extensions.PE;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using Alliance.Common.GameModels;
using Alliance.Common.Patch;
using Alliance.Common.Utilities;
using Alliance.Server.Core;
using Alliance.Server.Core.Configuration;
using Alliance.Server.Core.Configuration.Behaviors;
using Alliance.Server.Core.Database.Data;
using Alliance.Server.Core.Security;
using Alliance.Server.Core.Security.Behaviors;
using Alliance.Server.Extensions.AdminMenu.Behaviors;
using Alliance.Server.Extensions.AIBehavior.Behaviors;
using Alliance.Server.Extensions.Animals.Behaviors;
using Alliance.Server.Extensions.DieUnderWater.Behaviors;
using Alliance.Server.Extensions.FakeArmy.Behaviors;
using Alliance.Server.Extensions.PlayerSpawn.Behaviors;
using Alliance.Server.Extensions.ToggleEntities.Behaviors;
using Alliance.Server.Extensions.TroopSpawner.Behaviors;
using Alliance.Server.Extensions.Zevent.Behavior;
using Alliance.Server.GameModes.BattleRoyale;
using Alliance.Server.GameModes.BattleX;
using Alliance.Server.GameModes.Groupfight;
using Alliance.Server.GameModes.CaptainX;
using Alliance.Server.GameModes.CvC;
using Alliance.Server.GameModes.DeathmatchX;
using Alliance.Server.GameModes.Halloween;
using Alliance.Server.GameModes.Lobby;
using Alliance.Server.GameModes.PvC;
using Alliance.Server.GameModes.SiegeX;
using Alliance.Server.GameModes.Story;
using Alliance.Server.GameModes.Story.Actions;
using Alliance.Server.GameModes.TeamDeathmatchX;
using Alliance.Server.Patch;
using Alliance.Server.Patch.Behaviors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.Zevent.Behaviors;
using Alliance.Common.GameModes.Story.Behaviors;


using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server
{
	public class SubModule : MBSubModuleBase
	{
		public const string ModuleId = "Alliance.Server";
        public const string PlayerStorePath = "./alliance_players.txt";
        public const string ConfigFilePath = "./alliance_config.txt";
        public const string PlayerSpawnMenuFilePath = "spawn_preset_lobby_inf.xml";
        public const string BanHistoryFilePath = "./alliance_AllBans.txt";

        protected override void OnSubModuleLoad()
		{
			// Initialize player roles and access level

			PlayerStore.Instance.InitFromFile(PlayerStorePath);

			Server_ActionFactory.Initialize();
            // Init for Scenario

			// Initialize mod configuration
			ConfigInitializer.Init();

			// Apply Harmony patches
			DirtyCommonPatcher.Patch();
			DirtyServerPatcher.Patch();
            if (Config.Instance.ShowOfficers)
            {
                NicknameDatabase.Initialize();
                NicknameDatabase.StartAutoRefresh(TimeSpan.FromMinutes(1));
            }

            AddGameModes();
		}

		public override void OnBeforeMissionBehaviorInitialize(Mission mission)
		{
			// Initialize animation system and all the game animations
			AnimationSystem.Instance.Init();

			SceneList.Initialize();

			AddCommonBehaviors(mission);

			// Apply additional native fixes through MissionBehaviors

			Log("Alliance behaviors initialized.", LogLevel.Debug);
		}

		protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
		{
			// TODO : Check which limits still need to be increased after 1.2
			// Increase native network compression limits to prevent crashes
			DirtyCommonPatcher.IncreaseNativeLimits();

			// Add player connection watcher for auto-kick
			game.AddGameHandler<PlayerConnectionWatcher>();
		}

		public override void OnGameInitializationFinished(Game game)
		{
			// Load ExtendedCharacter.xml into usable ExtendedCharacterObjects
			ExtendedXMLLoader.Init();
			ScenarioManagerServer.Initialize();
			// Initialize the player spawn menu
			if (PlayerSpawnMenu.TryLoadFromFile(PlayerSpawnMenuFilePath, out PlayerSpawnMenu newMenu))
			{
				PlayerSpawnMenu.Instance = newMenu;
				Log($"Alliance - Loaded PlayerSpawnMenu succesfully with {PlayerSpawnMenu.Instance.Teams.Count} teams.", LogLevel.Information);
			}
			else
			{
				PlayerSpawnMenu.Instance = new PlayerSpawnMenu();
				Log($"Alliance - Failed to load PlayerSpawnMenu from {PlayerSpawnMenuFilePath}. Using default menu.", LogLevel.Warning);
			}
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarter)
		{
			// Add our custom GameModels 
			gameStarter.AddModel(new ExtendedAgentApplyDamageModel());
        }

		public override void OnGameEnd(Game game)
		{
			game.RemoveGameHandler<PlayerConnectionWatcher>();
		}

		private void AddGameModes()
		{
			Module.CurrentModule.AddMultiplayerGameMode(new LobbyGameMode("Lobby"));
			Module.CurrentModule.AddMultiplayerGameMode(new BRGameMode("BattleRoyale"));
			Module.CurrentModule.AddMultiplayerGameMode(new PvCGameMode("PvC"));
			Module.CurrentModule.AddMultiplayerGameMode(new CvCGameMode("CvC"));
			Module.CurrentModule.AddMultiplayerGameMode(new ScenarioGameMode("Scenario"));
			Module.CurrentModule.AddMultiplayerGameMode(new CaptainGameMode("CaptainX"));
			Module.CurrentModule.AddMultiplayerGameMode(new BattleGameMode("BattleX"));
			Module.CurrentModule.AddMultiplayerGameMode(new SiegeGameMode("SiegeX"));
            Module.CurrentModule.AddMultiplayerGameMode(new TeamDeathmatchGameMode("TeamDeathmatchX"));
            Module.CurrentModule.AddMultiplayerGameMode(new DeathmatchGameMode("DeathmatchX"));
            Module.CurrentModule.AddMultiplayerGameMode(new HalloweenGameMode("Halloween"));
            Module.CurrentModule.AddMultiplayerGameMode(new GroupfightGameMode("Groupfight"));
        }

		private void AddCommonBehaviors(Mission mission)
        {
            mission.AddMissionBehavior(new CoreBehavior());
            mission.AddMissionBehavior(new ServerAutoHandler());
            mission.AddMissionBehavior(new SyncConfigBehavior());
            mission.AddMissionBehavior(new AllianceLobbyComponent());
            mission.AddMissionBehavior(new SyncPlayerStoreBehavior());
            //mission.AddMissionBehavior(new PlayerSpawnBehavior());
            mission.AddMissionBehavior(new UsableEntityBehavior());
			mission.AddMissionBehavior(new TroopSpawnerBehavior());
			mission.AddMissionBehavior(new BattlePowerCalculationLogic());
			//mission.AddMissionBehavior(new ALGlobalAIBehavior());
			mission.AddMissionBehavior(new DieUnderWaterBehavior());
			mission.AddMissionBehavior(new FakeArmyBehavior());
			mission.AddMissionBehavior(new RespawnBehavior());
            mission.AddMissionBehavior(new SyncMoveableArty());
            mission.AddMissionBehavior(new FirearmsMissionLogic());
			mission.AddMissionBehavior(new PrefabPlacementMissionLogic());
            //mission.AddMissionBehavior(new AnimalBehavior());
            mission.AddMissionBehavior(new ConditionsBehavior());
            mission.AddMissionBehavior(new ToggleEntitiesBehavior());
            mission.AddMissionBehavior(new NotAllPlayersJoinFixBehavior());
        }
	}
}