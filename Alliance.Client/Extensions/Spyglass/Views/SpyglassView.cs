using Alliance.Client.Extensions.Spyglass.ViewModels;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.Extensions.Spyglass.Views
{
	[DefaultView]
	public class SpyglassView : MissionView
	{
		private const string SpyglassItemId = "nwf_officer_spyglass";
		private GauntletLayer _layer;
		private SpyglassMaskVM _dataSource;

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			ViewOrderPriority = 100;
			_dataSource = new SpyglassMaskVM();
			_layer = new GauntletLayer("SpyglassMask", ViewOrderPriority, false);
			_layer.LoadMovie("SpyglassMask", _dataSource);
			MissionScreen.AddLayer(_layer);
		}

		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
		}

		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);

			Agent agent = Mission?.MainAgent;
			bool hasSpyglassEquipped = agent != null
				&& agent.IsActive()
				&& !agent.WieldedWeapon.IsEmpty
				&& agent.WieldedWeapon.Item?.StringId == SpyglassItemId;
			bool isZoomHeld = MissionScreen?.SceneLayer?.Input?.IsGameKeyDown(CombatHotKeyCategory.Zoom) ?? false;
			_dataSource.IsVisible = hasSpyglassEquipped && isZoomHeld;
		}

		public override void OnMissionScreenFinalize()
		{
			if (_layer != null)
			{
				MissionScreen.RemoveLayer(_layer);
				_layer = null;
			}

			_dataSource?.OnFinalize();
			_dataSource = null;
			base.OnMissionScreenFinalize();
		}
	}
}
