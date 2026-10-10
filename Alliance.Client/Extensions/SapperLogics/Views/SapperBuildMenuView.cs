using Alliance.Client.Extensions.SapperLogics.ViewModels;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.KeyBinder;
using Alliance.Common.Core.KeyBinder.Models;
using Alliance.Common.Extensions.CombatLogic;
using Alliance.Common.Extensions.SapperLogics;
using Alliance.Common.Extensions.SapperLogics.NetworkMessages.FromClient;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.SapperLogics.Views
{
    [DefaultView]
    public sealed class SapperBuildMenuView : MissionView, IUseKeyBinder
    {
        private const string KeyCategoryId = "nwf_sapper_building";
        private const string BuildMenuKeyId = "key_sapper_buildmenu";

        private GameKey _buildMenuKey;
        private GauntletLayer _layer;
        private SapperBuildMenuVM _dataSource;
        private bool _buildMenuKeyWasDown;

        private bool IsMenuOpen => _layer != null;

        BindedKeyCategory IUseKeyBinder.BindedKeys => new BindedKeyCategory
        {
            CategoryId = KeyCategoryId,
            Category = "NWF - Sapper Building",
            Keys = new List<BindedKey>
            {
                new BindedKey
                {
                    Id = BuildMenuKeyId,
                    Name = "Sapper Building Menu",
                    Description = "Hold to open the sapper building radial menu while wielding the sapper hammer.",
                    DefaultInputKey = InputKey.B
                }
            }
        };

        public override void EarlyStart()
        {
            base.EarlyStart();
            ResolveBuildMenuKey();
        }

        public override void OnMissionScreenFinalize()
        {
            CloseMenu();
            base.OnMissionScreenFinalize();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);

            if (GameNetwork.IsDedicatedServer)
            {
                return;
            }

            Agent mainAgent = Agent.Main;
            bool canOpenMenu = PrefabPlacementMissionLogic.CanAgentUseSapperBuildingMenu(mainAgent);

            if (IsMenuOpen)
            {
                BattleSideEnum teamSide = mainAgent?.Team?.Side ?? BattleSideEnum.None;
                PrefabPlacementMissionLogic placementLogic = Mission?.GetMissionBehavior<PrefabPlacementMissionLogic>();
                _dataSource?.SetBuildPoints(placementLogic?.GetSapperBuildingPoints(teamSide) ?? Config.Instance.SapperBuildingPoints);

                if (!canOpenMenu)
                {
                    CloseMenu();
                    return;
                }

                bool keyDown = IsBuildMenuKeyDown();
                if (IsBuildMenuKeyReleased() || (_buildMenuKeyWasDown && !keyDown))
                {
                    _dataSource?.CommitFocusedOption();
                    if (IsMenuOpen)
                    {
                        CloseMenu();
                    }
                    return;
                }

                _buildMenuKeyWasDown = keyDown;
                return;
            }

            if (canOpenMenu && IsBuildMenuKeyPressed())
            {
                _buildMenuKeyWasDown = true;
                OpenMenu();
            }
        }

        private void ResolveBuildMenuKey()
        {
            GameKeyContext category = HotKeyManager.GetCategory(KeyCategoryId);
            _buildMenuKey = category?.RegisteredGameKeys?.Find(key => key != null && key.StringId == BuildMenuKeyId);
        }

        private bool IsBuildMenuKeyDown()
        {
            if (_buildMenuKey == null)
            {
                ResolveBuildMenuKey();
            }

            return _buildMenuKey != null &&
                   (IsInputKeyDown(_buildMenuKey.KeyboardKey.InputKey) || IsInputKeyDown(_buildMenuKey.ControllerKey.InputKey));
        }

        private bool IsBuildMenuKeyPressed()
        {
            if (_buildMenuKey == null)
            {
                ResolveBuildMenuKey();
            }

            return _buildMenuKey != null &&
                   (IsInputKeyPressed(_buildMenuKey.KeyboardKey.InputKey) || IsInputKeyPressed(_buildMenuKey.ControllerKey.InputKey));
        }

        private bool IsBuildMenuKeyReleased()
        {
            if (_buildMenuKey == null)
            {
                ResolveBuildMenuKey();
            }

            return _buildMenuKey != null &&
                   (IsInputKeyReleased(_buildMenuKey.KeyboardKey.InputKey) || IsInputKeyReleased(_buildMenuKey.ControllerKey.InputKey));
        }

        private bool IsInputKeyDown(InputKey inputKey)
        {
            return inputKey != InputKey.Invalid && Mission?.InputManager?.IsKeyDown(inputKey) == true;
        }

        private bool IsInputKeyPressed(InputKey inputKey)
        {
            return inputKey != InputKey.Invalid && Mission?.InputManager?.IsKeyPressed(inputKey) == true;
        }

        private bool IsInputKeyReleased(InputKey inputKey)
        {
            return inputKey != InputKey.Invalid && Mission?.InputManager?.IsKeyReleased(inputKey) == true;
        }

        private void OpenMenu()
        {
            try
            {
                _dataSource = new SapperBuildMenuVM(
                    SapperBuildingCatalog.GetPrefabIds(),
                    GetCurrentTeamBuildPoints(),
                    OnBuildRequested,
                    CloseMenu);

                _layer = new GauntletLayer("SapperBuildRadialMenu", 25, false);
                _layer.InputRestrictions.SetInputRestrictions();

                SpriteData spriteData = UIResourceManager.SpriteData;
                TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
                var uiResourceDepot = UIResourceManager.ResourceDepot;
                spriteData.SpriteCategories["ui_order"].Load(resourceContext, uiResourceDepot);

                _layer.LoadMovie("SapperBuildMenu", _dataSource);

                MissionScreen.AddLayer(_layer);
                ScreenManager.TrySetFocus(_layer);
            }
            catch (Exception ex)
            {
                Log("Alliance - Error opening sapper building menu:", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
                CloseMenu();
            }
        }

        private int GetCurrentTeamBuildPoints()
        {
            BattleSideEnum teamSide = Agent.Main?.Team?.Side ?? BattleSideEnum.None;
            PrefabPlacementMissionLogic placementLogic = Mission?.GetMissionBehavior<PrefabPlacementMissionLogic>();
            return placementLogic?.GetSapperBuildingPoints(teamSide) ?? Config.Instance.SapperBuildingPoints;
        }

        private void CloseMenu()
        {
            _buildMenuKeyWasDown = false;
            if (_layer != null)
            {
                _layer.InputRestrictions.ResetInputRestrictions();
                MissionScreen.RemoveLayer(_layer);
                _layer = null;
            }

            _dataSource?.OnFinalize();
            _dataSource = null;
        }

        private void OnBuildRequested(string prefabId)
        {
            if (!IsMenuOpen)
            {
                return;
            }

            Agent mainAgent = Agent.Main;
            if (GameNetwork.IsClient &&
                PrefabPlacementMissionLogic.CanAgentUseSapperBuildingMenu(mainAgent) &&
                SapperBuildingCatalog.ContainsPrefab(prefabId))
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new RequestSapperBuilding(prefabId));
                GameNetwork.EndModuleEventAsClient();
            }

            CloseMenu();
        }
    }
}
