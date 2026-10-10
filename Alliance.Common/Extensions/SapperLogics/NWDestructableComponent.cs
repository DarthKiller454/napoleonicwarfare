using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;

namespace Alliance.Common.Extensions.Construction.Scripts
{
    /// <summary>
    /// Server-authoritative sapper construction component.
    ///
    /// Optional construct/built child names let a prefab use separate meshes and physics
    /// bodies for each stage.
    /// </summary>
    public class ConstructableDestructableComponent : DestructableComponent
    {
        private static readonly CompressionInfo.Integer StateCompressionInfo =
            new CompressionInfo.Integer(0, 2, true);
        private static readonly CompressionInfo.Integer BuildProgressCompressionInfo =
            new CompressionInfo.Integer(0, 1000000, true);

        public string SapperToolId = "nwf_sapper_hammer";
        public float BuildHitPoint = 1000f;
        public float BuildAmountPerHit = 100f;
        public ConstructableState InitialState = ConstructableState.Construct;

        // Empty names keep an existing prefab's visuals unchanged until it has state meshes.
        public string ConstructMeshEntityName = string.Empty;
        public string BuiltMeshEntityName = string.Empty;

        private ConstructableState _state;
        private float _buildProgress;
        private WeakGameEntity _constructMeshEntity = WeakGameEntity.Invalid;
        private WeakGameEntity _builtMeshEntity = WeakGameEntity.Invalid;

        public ConstructableDestructableComponent()
        {
        }

        public ConstructableState State => _state;
        public float BuildProgress => _buildProgress;

        protected override void OnInit()
        {
            base.OnInit();

            BuildHitPoint = Math.Max(1f, BuildHitPoint);
            BuildAmountPerHit = Math.Max(1f, BuildAmountPerHit);
            MaxHitPoint = Math.Max(1f, MaxHitPoint);
            CacheStateMeshes();
            ApplyState(InitialState, synchronize: false);
        }

        protected override void OnMissionReset()
        {
            base.OnMissionReset();
            CacheStateMeshes();
            ApplyState(InitialState, synchronize: false);
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
            out float modifiedDamage,
            out float fireDamage,
            out float modifiedFireDamage)
        {
            reportDamage = false;
            modifiedDamage = 0f;
            fireDamage = 0f;
            modifiedFireDamage = 0f;

            // Construction and destruction are authoritative on the server. State changes
            // use SyncObjectState/readable records, while live health uses SyncObjectHitpoints.
            if (GameNetwork.IsClientOrReplay || _state == ConstructableState.Dismantled)
            {
                return true;
            }

            if (_state == ConstructableState.Construct)
            {
                if (IsSapperTool(weapon))
                {
                    AddBuildProgress();
                    return true;
                }

                // Damage to an unfinished prop removes construction progress at the same
                // rate as hit points. The prop is removed only when its health reaches zero.
                if (inflictedDamage > 0)
                {
                    reportDamage = true;
                    modifiedDamage = inflictedDamage;
                    DamageConstruction(inflictedDamage);
                }

                return true;
            }

            if (IsSapperTool(weapon))
            {
                RepairBuiltProp();
                return true;
            }

            // Built props take ordinary damage. When that damage would destroy them, turn
            // them back into an unfinished prop instead of allowing native removal logic.
            if (inflictedDamage > 0 && HitPoint <= inflictedDamage)
            {
                reportDamage = true;
                modifiedDamage = inflictedDamage;
                ApplyState(ConstructableState.Construct, synchronize: true);
                return true;
            }

            bool hitHandled = base.OnHit(
                attackerAgent,
                inflictedDamage,
                impactPosition,
                impactDirection,
                in weapon,
                affectorWeaponSlotOrMissileIndex,
                attackerScriptComponentBehavior,
                out reportDamage,
                out modifiedDamage,
                out fireDamage,
                out modifiedFireDamage);

            SynchronizeHitPoints();
            return hitHandled;
        }

        private bool IsSapperTool(in MissionWeapon weapon)
        {
            return !weapon.IsEmpty && weapon.Item != null &&
                   weapon.Item.StringId == SapperToolId;
        }

        private void AddBuildProgress()
        {
            _buildProgress = Math.Min(BuildHitPoint, _buildProgress + BuildAmountPerHit);
            HitPoint = GetHitPointsForBuildProgress(_buildProgress);

            if (_buildProgress >= BuildHitPoint)
            {
                ApplyState(ConstructableState.Built, synchronize: true);
            }
            else
            {
                SynchronizeHitPoints();
            }
        }

        private void DamageConstruction(float damage)
        {
            float remainingHitPoints = Math.Max(0f, HitPoint - damage);
            if (remainingHitPoints <= 0f)
            {
                ApplyState(ConstructableState.Dismantled, synchronize: true);
                return;
            }

            HitPoint = remainingHitPoints;
            _buildProgress = Math.Max(0f, Math.Min(BuildHitPoint,
                remainingHitPoints / MaxHitPoint * BuildHitPoint));
            SynchronizeHitPoints();
        }

        private void RepairBuiltProp()
        {
            if (HitPoint >= MaxHitPoint)
            {
                return;
            }

            // Repair at the same percentage rate that one hammer hit contributes while
            // constructing the object (10% per hit with the default prefab values).
            float repairAmount = MaxHitPoint * BuildAmountPerHit / BuildHitPoint;
            HitPoint = Math.Min(MaxHitPoint, HitPoint + Math.Max(1f, repairAmount));
            SynchronizeHitPoints();
        }

        private float GetHitPointsForBuildProgress(float progress)
        {
            // Keep a fresh construct alive for hit processing while displaying its small
            // initial health value instead of treating zero health as already destroyed.
            return Math.Max(1f, MaxHitPoint * progress / BuildHitPoint);
        }

        private void SynchronizeHitPoints()
        {
            if (GameNetwork.IsServerOrRecorder)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncObjectHitpoints(Id, HitPoint));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
            }
        }

        private void CacheStateMeshes()
        {
            _constructMeshEntity = WeakGameEntity.Invalid;
            _builtMeshEntity = WeakGameEntity.Invalid;

            if (string.IsNullOrWhiteSpace(ConstructMeshEntityName) &&
                string.IsNullOrWhiteSpace(BuiltMeshEntityName))
            {
                return;
            }

            foreach (WeakGameEntity child in GameEntity.GetChildren())
            {
                if (!_constructMeshEntity.IsValid && child.Name == ConstructMeshEntityName)
                {
                    _constructMeshEntity = child;
                }

                if (!_builtMeshEntity.IsValid && child.Name == BuiltMeshEntityName)
                {
                    _builtMeshEntity = child;
                }
            }
        }

        private void ApplyState(ConstructableState state, bool synchronize)
        {
            if (!Enum.IsDefined(typeof(ConstructableState), state))
            {
                state = ConstructableState.Construct;
            }

            _state = state;
            switch (_state)
            {
                case ConstructableState.Construct:
                    _buildProgress = 0f;
                    HitPoint = 1f;
                    break;
                case ConstructableState.Built:
                    _buildProgress = BuildHitPoint;
                    HitPoint = MaxHitPoint;
                    break;
                case ConstructableState.Dismantled:
                    _buildProgress = 0f;
                    HitPoint = 0f;
                    break;
            }

            if (_state == ConstructableState.Dismantled)
            {
                GameEntity.SetPhysicsState(false, true);
                if (synchronize)
                {
                    SetVisibleSynched(false, true);
                }
                else
                {
                    GameEntity.SetVisibilityExcludeParents(false);
                }
            }
            else
            {
                GameEntity.SetVisibilityExcludeParents(true);
                GameEntity.SetPhysicsState(true, true);
            }

            // Enabling physics on the root also enables child bodies. Reapply the per-state
            // choice after that so exactly one stage body remains active.
            ApplyVisualState();

            if (synchronize)
            {
                SynchronizeHitPoints();
            }

            if (synchronize && _state != ConstructableState.Dismantled &&
                GameNetwork.IsServerOrRecorder)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncObjectState(Id, (int)_state));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
            }
        }

        private void ApplyVisualState()
        {
            if (_constructMeshEntity.IsValid)
            {
                bool isConstructState = _state == ConstructableState.Construct;
                _constructMeshEntity.SetVisibilityExcludeParents(isConstructState);
                _constructMeshEntity.SetPhysicsState(isConstructState, true);
            }

            if (_builtMeshEntity.IsValid)
            {
                bool isBuiltState = _state == ConstructableState.Built;
                _builtMeshEntity.SetVisibilityExcludeParents(isBuiltState);
                _builtMeshEntity.SetPhysicsState(isBuiltState, true);
            }
        }

        /// <summary>Applies a live state update received from the server without echoing it.</summary>
        public void ApplySynchronizedState(int stateIndex)
        {
            if (stateIndex < (int)ConstructableState.Construct ||
                stateIndex > (int)ConstructableState.Dismantled)
            {
                return;
            }

            CacheStateMeshes();
            ApplyState((ConstructableState)stateIndex, synchronize: false);
        }

        /// <summary>Applies authoritative health/progress updates received from the server.</summary>
        public void ApplySynchronizedHitPoints(float hitPoints)
        {
            HitPoint = Math.Max(0f, Math.Min(MaxHitPoint, hitPoints));

            if (_state == ConstructableState.Construct)
            {
                _buildProgress = Math.Max(0f, Math.Min(BuildHitPoint,
                    HitPoint / MaxHitPoint * BuildHitPoint));
            }
            else if (_state == ConstructableState.Built)
            {
                _buildProgress = BuildHitPoint;
            }

            if (HitPoint <= 0f)
            {
                _state = ConstructableState.Dismantled;
                _buildProgress = 0f;
                GameEntity.SetPhysicsState(false, true);
                GameEntity.SetVisibilityExcludeParents(false);
                ApplyVisualState();
            }
        }

        public override void WriteToNetwork()
        {
            base.WriteToNetwork();
            GameNetworkMessage.WriteIntToPacket((int)_state, StateCompressionInfo);
            int buildProgressInTenths = (int)Math.Round(Math.Max(0f, Math.Min(100000f, _buildProgress)) * 10f);
            GameNetworkMessage.WriteIntToPacket(buildProgressInTenths, BuildProgressCompressionInfo);
        }

        public override void OnAfterReadFromNetwork(
            (BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord) synchedMissionObjectReadableRecord,
            bool allowVisibilityUpdate = true)
        {
            ConstructableDestructableComponentRecord record =
                (ConstructableDestructableComponentRecord)synchedMissionObjectReadableRecord.Item2;

            // DestructableComponent writes its own readable record before our fields.
            // Pass the correctly typed base record to its reader rather than passing this
            // derived component's record, which the native implementation cannot cast.
            (BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord) baseReadableRecord =
                (synchedMissionObjectReadableRecord.Item1, record.DestructableRecord);
            base.OnAfterReadFromNetwork(baseReadableRecord, allowVisibilityUpdate);

            if (record.State < (int)ConstructableState.Construct ||
                record.State > (int)ConstructableState.Dismantled)
            {
                return;
            }

            CacheStateMeshes();
            _state = (ConstructableState)record.State;
            _buildProgress = Math.Max(0f, Math.Min(BuildHitPoint, record.BuildProgress));

            if (_state == ConstructableState.Construct)
            {
                HitPoint = GetHitPointsForBuildProgress(_buildProgress);
            }
            else if (_state == ConstructableState.Built)
            {
                _buildProgress = BuildHitPoint;
                HitPoint = Math.Max(0f, Math.Min(MaxHitPoint, HitPoint));
            }
            else
            {
                HitPoint = 0f;
            }

            GameEntity.SetPhysicsState(_state != ConstructableState.Dismantled, true);
            GameEntity.SetVisibilityExcludeParents(_state != ConstructableState.Dismantled);
            ApplyVisualState();
        }

        [DefineSynchedMissionObjectTypeForMod(typeof(ConstructableDestructableComponent))]
        public struct ConstructableDestructableComponentRecord : ISynchedMissionObjectReadableRecord
        {
            public DestructableComponent.DestructableComponentRecord DestructableRecord { get; private set; }
            public int State { get; private set; }
            public float BuildProgress { get; private set; }

            public bool ReadFromNetwork(ref bool bufferReadValid)
            {
                DestructableComponent.DestructableComponentRecord destructableRecord = default;
                destructableRecord.ReadFromNetwork(ref bufferReadValid);
                DestructableRecord = destructableRecord;

                if (!bufferReadValid)
                {
                    return false;
                }

                State = GameNetworkMessage.ReadIntFromPacket(StateCompressionInfo, ref bufferReadValid);
                BuildProgress = GameNetworkMessage.ReadIntFromPacket(BuildProgressCompressionInfo, ref bufferReadValid) / 10f;
                return bufferReadValid;
            }
        }
    }

    public enum ConstructableState
    {
        Construct,
        Built,
        Dismantled
    }
}
