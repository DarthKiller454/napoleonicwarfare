using Alliance.Common.Extensions.Artillery;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.Extensions.ExNativeUI.Artillery
{
    public sealed class CannonCrosshairMissionView : MissionView
    {
        private const int SwitchAlternativeUsageGameKey = 17;

        // Change this to your actual local-only marker prefab name.
        private const string DotPrefab = "nwf_cannon_crosshair_dot";

        private const int DotCount = 18;
        private const float DistanceStep = 18f; 
        private const float InitialMarkerDistance = 5f;

        // Bannerlord scale is close enough for this visual helper.
        private const float Gravity = 9.81f;

        // Small visual offset so the final terrain marker does not z-fight with terrain.
        private const float TerrainImpactVisualOffset = 0.08f;

        private readonly List<GameEntity> _dots = new(DotCount);

        private GauntletLayer _hintLayer;
        private CannonCrosshairHintVM _hintVM;

        private bool _crosshairEnabled = true;
        private MatrixFrame CreateMarkerFrame(Vec3 position, Vec3 pathDirection)
        {
            if (pathDirection.LengthSquared < 0.0001f)
            {
                MatrixFrame fallbackFrame = MatrixFrame.Identity;
                fallbackFrame.origin = position;
                return fallbackFrame;
            }

            pathDirection.Normalize();

            Mat3 rotation = Mat3.Identity;

            rotation.f = pathDirection;
            rotation.u = Vec3.Up;
            rotation.Orthonormalize();

            return new MatrixFrame(rotation, position);
        }
        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            if (GameNetwork.IsDedicatedServer)
                return;

            EnsureDots();
            SetDotsVisible(false);

            _hintVM = new CannonCrosshairHintVM();

            _hintLayer = new GauntletLayer("CannonCrosshairHintLayer", 50, false);
            _hintLayer.LoadMovie("CannonCrosshairHint", _hintVM);

            MissionScreen.AddLayer(_hintLayer);

            UpdateHintText();
            _hintVM.IsVisible = false;
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            HideCrosshairDots();

            if (_hintLayer != null)
            {
                MissionScreen.RemoveLayer(_hintLayer);
                _hintLayer = null;
            }

            _hintVM = null;

            RemoveDots();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);

            if (GameNetwork.IsDedicatedServer)
                return;

            NWF_Cannon_Moveable cannon = FindMainAgentAimedCannon();

            bool shouldShowHint = cannon != null;

            if (_hintVM != null)
                _hintVM.IsVisible = shouldShowHint;

            if (!shouldShowHint)
            {
                HideCrosshairDots();
                return;
            }

            if (IsSwitchAlternativeUsagePressed())
            {
                _crosshairEnabled = !_crosshairEnabled;
                UpdateHintText();

                if (!_crosshairEnabled)
                    HideCrosshairDots();
            }

            if (_crosshairEnabled)
            {
                UpdateCannonCrosshair(cannon);
            }
            else
            {
                HideCrosshairDots();
            }
        }

        private NWF_Cannon_Moveable FindMainAgentAimedCannon()
        {
            Agent main = Agent.Main;

            if (main == null || !main.IsActive())
                return null;

            return NWF_Cannon_Moveable.ActiveCannons
                .FirstOrDefault(cannon =>
                    cannon != null &&
                    cannon.GameEntity != null &&
                    cannon.GameEntity.IsValid &&
                    cannon.IsAimedByMainAgent());
        }

        private bool IsSwitchAlternativeUsagePressed()
        {
            if (MissionScreen?.SceneLayer?.Input != null)
                return MissionScreen.SceneLayer.Input.IsGameKeyPressed(SwitchAlternativeUsageGameKey);

            return Input.IsKeyPressed(InputKey.X);
        }

        private void UpdateHintText()
        {
            if (_hintVM == null)
                return;

            TextObject text = _crosshairEnabled
                ? new TextObject("{=!}{KEY} Hide Cannon Aiming Path")
                : new TextObject("{=!}{KEY} Show Cannon Aiming Path");

            text.SetTextVariable(
                "KEY",
                HyperlinkTexts.GetKeyHyperlinkText(
                    HotKeyManager.GetHotKeyId("CombatHotKeyCategory", SwitchAlternativeUsageGameKey)));

            _hintVM.HintText = text.ToString();
        }

        private void UpdateCannonCrosshair(NWF_Cannon_Moveable cannon)
        {
            if (cannon == null)
            {
                HideCrosshairDots();
                return;
            }

            if (!cannon.TryGetClientAimData(out Vec3 origin, out Vec3 direction, out float speed))
            {
                HideCrosshairDots();
                return;
            }

            if (speed <= 0.01f || direction.LengthSquared < 0.0001f)
            {
                HideCrosshairDots();
                return;
            }

            direction.Normalize();

            UpdateDots(origin, direction, speed);
        }

        private void EnsureDots()
        {
            if (_dots.Count >= DotCount)
                return;

            if (Mission.Current == null || Mission.Current.Scene == null)
                return;

            while (_dots.Count < DotCount)
            {
                MatrixFrame frame = MatrixFrame.Identity;
                GameEntity dot = GameEntity.Instantiate(Mission.Current.Scene, DotPrefab, frame);

                if (dot == null)
                    break;

                dot.SetVisibilityExcludeParents(false);
                _dots.Add(dot);
            }
        }

        private void UpdateDots(Vec3 origin, Vec3 direction, float speed)
        {
            EnsureDots();

            if (_dots.Count == 0)
                return;

            Vec3 previousPos = origin;

            for (int i = 0; i < _dots.Count; i++)
            {
                float distance = InitialMarkerDistance + i * DistanceStep;
                float time = distance / speed;

                Vec3 rawPos = origin + direction * distance;
                rawPos.z -= 0.5f * Gravity * time * time;

                Vec3 pathDirection = rawPos - previousPos;

                if (TryGetTerrainImpact(previousPos, rawPos, out Vec3 impactPos))
                {
                    Vec3 impactDirection = impactPos - previousPos;

                    MatrixFrame impactFrame = CreateMarkerFrame(impactPos, impactDirection);

                    _dots[i].SetGlobalFrame(impactFrame);
                    _dots[i].SetVisibilityExcludeParents(true);

                    HideDotsAfter(i + 1);
                    return;
                }

                MatrixFrame frame = CreateMarkerFrame(rawPos, pathDirection);

                _dots[i].SetGlobalFrame(frame);
                _dots[i].SetVisibilityExcludeParents(true);

                previousPos = rawPos;
            }
        }

        private bool TryGetTerrainImpact(Vec3 from, Vec3 to, out Vec3 impact)
        {
            impact = Vec3.Zero;

            if (Mission.Current == null || Mission.Current.Scene == null)
                return false;

            Scene scene = Mission.Current.Scene;

            float fromTerrainZ = scene.GetTerrainHeight(from.AsVec2);
            float toTerrainZ = scene.GetTerrainHeight(to.AsVec2);

            float fromDelta = from.z - fromTerrainZ;
            float toDelta = to.z - toTerrainZ;

            // Segment has not crossed the terrain surface.
            if (fromDelta < 0f || toDelta > 0f)
                return false;

            float denominator = fromDelta - toDelta;

            float t = denominator > 0.0001f
                ? fromDelta / denominator
                : 1f;

            t = MBMath.ClampFloat(t, 0f, 1f);

            impact = from + (to - from) * t;

            // Re-sample terrain at the interpolated XY position.
            impact.z = scene.GetTerrainHeight(impact.AsVec2) + TerrainImpactVisualOffset;

            return true;
        }

        private void HideCrosshairDots()
        {
            SetDotsVisible(false);
        }

        private void SetDotsVisible(bool visible)
        {
            for (int i = 0; i < _dots.Count; i++)
            {
                GameEntity dot = _dots[i];

                dot?.SetVisibilityExcludeParents(visible);
            }
        }

        private void HideDotsAfter(int index)
        {
            for (int i = index; i < _dots.Count; i++)
            {
                GameEntity dot = _dots[i];

                dot?.SetVisibilityExcludeParents(false);
            }
        }

        private void RemoveDots()
        {
            for (int i = 0; i < _dots.Count; i++)
            {
                GameEntity dot = _dots[i];

                dot?.Remove(0);
            }

            _dots.Clear();
        }
    }
}