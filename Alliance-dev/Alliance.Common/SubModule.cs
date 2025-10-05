using TaleWorlds.Core;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common
{
	public class SubModule : MBSubModuleBase
	{
		public const string ModuleId = "Alliance.Common";
		public static string CurrentModuleName = "Napoleonic Warfare MP";

		protected override void OnSubModuleLoad()
		{
			Log("Alliance.Common initialized", LogLevel.Debug);
            base.OnSubModuleLoad();
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
        }

		public override void OnGameInitializationFinished(Game game)
		{
        }
        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
        }
    }
}