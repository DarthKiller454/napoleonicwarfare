using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.Construction.Scripts
{
    public class ConstructableDestructableComponent : DestructableComponent
    {
        public string SapperToolId = "sapper_hammer";

        public float BuildHitPoint = 1000f;
        public float BuildAmountPerHit = 100f;

        public ConstructableState InitialState = ConstructableState.Construct;
        private ConstructableState _state;
        private float _buildProgress;

        public ConstructableDestructableComponent()
        {
        }

        protected override void OnInit()
        {
            base.OnInit();

            // Starts as "construct" instead of already built.
            _state = InitialState;
            if (_state == ConstructableState.Construct)
            {
                _buildProgress = 0f;
                HitPoint = 4;
            }
            else if (_state == ConstructableState.Built)
            {
                _buildProgress = BuildHitPoint;
                HitPoint = MaxHitPoint;
            }

            // Important:
            // We do NOT want native destruction logic to consider this permanently destroyed.
        }

        protected override bool OnHit(
            Agent attackerAgent,
            int inflictedDamage,
            Vec3 impactPosition,
            Vec3 impactDirection,
            in MissionWeapon weapon,
            int affectorWeaponSlotOrMissileIndex,
            ScriptComponentBehavior attackerScriptComponentBehavior,
            out bool reportDamage,
            out float modifiedDamage)
        {
            reportDamage = false;
            modifiedDamage = inflictedDamage;

            bool isSapperHit = IsSapperTool(weapon);

            if (_state == ConstructableState.Construct)
            {
                if (isSapperHit)
                {
                    AddBuildProgress(attackerAgent);
                    reportDamage = false;
                    modifiedDamage = 0f;
                    float _buildPercentage = _buildProgress / BuildHitPoint * 100;
                    InformationManager.DisplayMessage(new InformationMessage($"Build progress: {_buildPercentage}%"));
                    // Consume the hit here.
                    return true;
                }

                // Conventional damage against unfinished construct = dismantle/remove.
                Dismantle();
                reportDamage = true;
                modifiedDamage = inflictedDamage;
                return true;
            }

            if (_state == ConstructableState.Built)
            {
                // Normal object destruction.
                return base.OnHit(
                    attackerAgent,
                    inflictedDamage,
                    impactPosition,
                    impactDirection,
                    in weapon,
                    affectorWeaponSlotOrMissileIndex,
                    attackerScriptComponentBehavior,
                    out reportDamage,
                    out modifiedDamage);
            }

            // Already dismantled / removed.
            reportDamage = false;
            modifiedDamage = 0f;
            return true;
        }

        private bool IsSapperTool(in MissionWeapon weapon)
        {
            if (weapon.IsEmpty || weapon.Item == null)
                return false;

            return weapon.Item.StringId == SapperToolId;
        }

        private void AddBuildProgress(Agent builderAgent)
        {
            _buildProgress += BuildAmountPerHit;

            if (_buildProgress >= BuildHitPoint)
            {
                FinishConstruction();
            }
        }

        private void FinishConstruction()
        {
            _state = ConstructableState.Built;

            // Restore native destructible state.
            Reset();
            HitPoint = MaxHitPoint;

            // Optional:
            // You can trigger particles/sound here.
        }

        private void Dismantle()
        {
            _state = ConstructableState.Dismantled;

            SetVisibleSynched(false, true);
            GameEntity.SetPhysicsState(false, true);
        }
    }

    public enum ConstructableState
    {
        Construct,
        Built,
        Dismantled
    }
}