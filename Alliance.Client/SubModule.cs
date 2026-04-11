using Alliance.Client.Core;
using Alliance.Client.Extensions.CustomName;
using Alliance.Client.Extensions.CustomScripts.Handlers;
using Alliance.Client.Extensions.FakeArmy.Behaviors;
using Alliance.Client.Extensions.VOIP.Behaviors;
using Alliance.Client.GameModes.BattleRoyale;
using Alliance.Client.GameModes.BattleX;
using Alliance.Client.GameModes.CaptainX;
using Alliance.Client.GameModes.CvC;
using Alliance.Client.GameModes.DeathmatchX;
using Alliance.Client.GameModes.Groupfight;
using Alliance.Client.GameModes.Halloween;
using Alliance.Client.GameModes.Lobby;
using Alliance.Client.GameModes.PvC;
using Alliance.Client.GameModes.SiegeX;
using Alliance.Client.GameModes.Story;
using Alliance.Client.GameModes.Story.Actions;
using Alliance.Client.GameModes.TeamDeathmatchX;
using Alliance.Client.Patch;
using Alliance.Client.Patch.Behaviors;
using Alliance.Client.Patch.HarmonyPatch;
using Alliance.Common.Core.ExtendedXML;
using Alliance.Common.Core.KeyBinder;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.CombatLogic;
using Alliance.Common.Extensions.PE;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using Alliance.Common.GameModels;
using Alliance.Common.Patch;
using Alliance.Common.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using static Alliance.Common.Utilities.Logger;
using Module = TaleWorlds.MountAndBlade.Module;

namespace Alliance.Client
{
	public class SubModule : MBSubModuleBase
	{
		public const string ModuleId = "Alliance.Client";

		protected override void OnSubModuleLoad()
		{
            // Register and initialize Key Binder
            List<Assembly> assemblies = new List<Assembly>
            {
                Assembly.GetAssembly(typeof(Common.SubModule)),
                Assembly.GetAssembly(typeof(Client.SubModule))
            };
            KeyBinder.Initialize(assemblies);

            Client_ActionFactory.Initialize();

            // Apply Harmony patches
            DirtyCommonPatcher.Patch();
            DirtyClientPatcher.Patch();

            KeyBinder.RegisterContexts();

            AddGameModes();
        }

		protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
		{
			DirtyCommonPatcher.IncreaseNativeLimits();
            AnimationSystem.Instance.Init();
        }

		public override void OnBeforeMissionBehaviorInitialize(Mission mission)
		{

			AddCommonBehaviors(mission);

			Log("Alliance behaviors initialized.", LogLevel.Debug);
		}

		public override void OnGameInitializationFinished(Game game)
        {
            ExtendedXMLLoader.Init();
            SceneList.Initialize();
            ScenarioPlayer.Initialize();
        }
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            _ = ClientNicknameManager.InitializeAsync();
        }
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
		{
            gameStarter.AddModel(new ExtendedAgentApplyDamageModel());
            gameStarter.AddModel(new ExtendedAgentStatCalculateModel());
        }

		private void AddGameModes()
		{
			Module.CurrentModule.AddMultiplayerGameMode(new LobbyGameMode("Lobby"));
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

        /// <summary>
        /// Add common behaviors from Alliance used by all GameModes.
        /// </summary>
        public void AddCommonBehaviors(Mission mission)
        {
            mission.AddMissionBehavior(new CoreBehavior());
            mission.AddMissionBehavior(new ClientAutoHandler());
			mission.AddMissionBehavior(new UsableEntityBehavior());
			//mission.AddMissionBehavior(new PBVoiceChatHandlerClient());
			mission.AddMissionBehavior(new FakeArmyBehavior());
            mission.AddMissionBehavior(new SyncMoveableArty());
			mission.AddMissionBehavior(new FirearmsMissionLogic());
            mission.AddMissionBehavior(new PrefabPlacementMissionLogic());
            //mission.AddMissionBehavior(new PrefabDeletionLogic()); might become deprecated
            mission.AddMissionBehavior(new AllianceAgentVisualSpawnComponent());
        }
    }
}