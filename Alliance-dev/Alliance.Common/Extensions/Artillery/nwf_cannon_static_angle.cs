using Alliance.Common.Extensions.Artillery;
using Alliance.Common.Extensions.PE;
using Alliance.Common.Extensions.Vehicles.Scripts;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Diamond;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.Artillery
{
    public sealed class PE_MangonelAI_StaticAngle : RangedSiegeWeaponAi
    {
        public PE_MangonelAI_StaticAngle(NWF_Cannon_StaticAngle mangonel) : base(mangonel)
        {
        }
    }
    public class NWF_Cannon_StaticAngle : RangedSiegeWeapon, ISpawnable, IMoveable
    {
        private MatrixFrame starterPosition;
        protected override float MaximumBallisticError
        {
            get
            {
                return 0.4f;
            }
        }
        public override float DirectionRestriction
        {
            get
            {
                return 100f;
            }
        }

        protected override float ShootingSpeed
        {
            get
            {
                return this.ProjectileSpeed;
            }
        }
        protected override void OnMissionReset()
        {
            base.OnMissionReset();
            Projectile.GameEntity.SetVisibilityExcludeParents(visible: true);
            foreach (StandingPoint standingPoint in base.StandingPoints)
            {
                standingPoint.UserAgent?.StopUsingGameObject();
                standingPoint.IsDeactivated = false;
            }

            currentDirection = (_lastSyncedDirection = 0f);
            _syncTimer = 0f;
            currentReleaseAngle = (_lastSyncedReleaseAngle = ReleaseAngleRestrictionCenter);
            targetDirection = currentDirection;
            targetReleaseAngle = currentReleaseAngle;
            _timeElapsedAfterMessage = 0f;
            ApplyCurrentDirectionToEntity();
            AmmoCount = startingAmmoCount;
            UpdateAmmoMesh();
            _ramrodhittime = 0;
            if (MoveSound != null)
            {
                MoveSound.Stop();
                MoveSound = null;
            }

            hasFrameChangedInPreviousFrame = false;
            Skeleton[] skeletons = Skeletons;
            for (int i = 0; i < skeletons.Length; i++)
            {
                skeletons[i].Freeze(p: false);
            }

            foreach (StandingPointWithWeaponRequirement ammoPickUpStandingPoint in AmmoPickUpStandingPoints)
            {
                ammoPickUpStandingPoint.IsDeactivated = false;
            }

            for (int i = 0; i < this.Skeletons.Length; i++)
            {
                this.Skeletons[i].SetAnimationAtChannel(this.SetUpAnimations[i], 0, 1f, 0f, 0f);
                this.Skeletons[i].SetAnimationParameterAtChannel(0, 1f);
                this.Skeletons[i].TickAnimations(0.0001f, MatrixFrame.Identity, true);
            }
            UpdateProjectilePosition();
            if (!GameNetwork.IsClientOrReplay)
            {
                SetActivationLoadAmmoPoint(activate: false);
            }
        }

        protected override void RegisterAnimationParameters()
        {
            this.SkeletonOwnerObjects = new SynchedMissionObject[2];
            this.Skeletons = new Skeleton[2];
            this.SkeletonNames = new string[1];
            this.FireAnimations = new string[2];
            this.FireAnimationIndices = new int[2];
            this.SetUpAnimations = new string[2];
            this.SetUpAnimationIndices = new int[2];
            this.SkeletonOwnerObjects[0] = this._body;
            this.Skeletons[0] = this._body.GameEntity.Skeleton;
            this.SkeletonNames[0] = this.MangonelBodySkeleton;
            this.FireAnimations[0] = this.MangonelBodyFire;
            this.FireAnimationIndices[0] = MBAnimation.GetAnimationIndexWithName(this.MangonelBodyFire);
            this.SetUpAnimations[0] = this.MangonelBodyReload;
            this.SetUpAnimationIndices[0] = MBAnimation.GetAnimationIndexWithName(this.MangonelBodyReload);
            this.SkeletonOwnerObjects[1] = this._rope;
            this.Skeletons[1] = this._rope.GameEntity.Skeleton;
            this.FireAnimations[1] = this.MangonelRopeFire;
            this.FireAnimationIndices[1] = MBAnimation.GetAnimationIndexWithName(this.MangonelRopeFire);
            this.SetUpAnimations[1] = this.MangonelRopeReload;
            this.SetUpAnimationIndices[1] = MBAnimation.GetAnimationIndexWithName(this.MangonelRopeReload);
            this._missileBoneName = this.ProjectileBoneName;
            this._idleAnimationActionIndex = ActionIndexCache.Create(this.IdleActionName);
            this._shootAnimationActionIndex = ActionIndexCache.Create(this.ShootActionName);
            this._reload1AnimationActionIndex = ActionIndexCache.Create(this.Reload1ActionName);
            this._reload2AnimationActionIndex = ActionIndexCache.Create(this.Reload2ActionName);
            this._rotateLeftAnimationActionIndex = ActionIndexCache.Create(this.RotateLeftActionName);
            this._rotateRightAnimationActionIndex = ActionIndexCache.Create(this.RotateRightActionName);
            this._loadAmmoBeginAnimationActionIndex = ActionIndexCache.Create(this.LoadAmmoBeginActionName);
            this._loadAmmoEndAnimationActionIndex = ActionIndexCache.Create(this.LoadAmmoEndActionName);
            this._reload2IdleActionIndex = ActionIndexCache.Create(this.Reload2IdleActionName);
        }

        public override UsableMachineAIBase CreateAIBehaviorObject()
        {
            return new PE_MangonelAI_StaticAngle(this);
        }

        public override void AfterMissionStart()
        {

            this.UpdateProjectilePosition();
        }

        public override SiegeEngineType GetSiegeEngineType()
        {
            if (this._defaultSide != BattleSideEnum.Attacker)
            {
                return DefaultSiegeEngineTypes.Catapult;
            }
            return DefaultSiegeEngineTypes.Onager;
        }

        protected override void UpdateAmmoMesh()
        {
        }
        void ResetPosition(object sender, PropertyChangedEventArgs e)
        {
            SyncFrame(starterPosition);
        }
        public virtual MatrixFrame SyncFrame(MatrixFrame frame)
        {
            if (frame.origin != GameEntity.GetFrame().origin || frame.rotation != GameEntity.GetFrame().rotation)
            {
                SetFrameSynched(ref frame, GameNetwork.IsClient);
            }

            return frame;
        }
        protected override void OnInit()
        {
            this.AmmoPickUpTag = null;
            List<SynchedMissionObject> list = base.GameEntity.CollectObjectsWithTag<SynchedMissionObject>("rope");
            if (list.Count > 0)
            {
                this._rope = list[0];
            }
            list = base.GameEntity.CollectObjectsWithTag<SynchedMissionObject>("body");
            this._body = list.Count > 0 ? list[0] : this;
            this.RotationObject = this._body;
            List<GameEntity> list2 = base.GameEntity.CollectChildrenEntitiesWithTag("vertical_adjuster");
            this._verticalAdjuster = list2[0];
            if (this._verticalAdjuster.Skeleton != null)
            {
                this._verticalAdjuster.Skeleton.SetAnimationAtChannel(this.MangonelAimAnimation, 0, 1f, -1f, 0f);
            }
            this._verticalAdjusterStartingLocalFrame = this._verticalAdjuster.GetFrame();
            this._verticalAdjusterStartingLocalFrame = this._body.GameEntity.GetBoneEntitialFrameWithIndex(0).TransformToLocal(this._verticalAdjusterStartingLocalFrame);
            base.OnInit();
            this.InitiateMoveSynch();
            starterPosition = GameEntity.GetFrame();
            Mission.Current.OnMissionReset += ResetPosition;
            this.HitPoint = this.MaxHitPoint;
            this.timeGapBetweenShootActionAndProjectileLeaving = 0.01f;
            this.timeGapBetweenShootingEndAndReloadingStart = 2f;
            this._ramrodhittime = 0;
            this._timeElapsedAfterMessage = 0f;
            this._rotateStandingPoints = new List<StandingPoint>();
            if (this.StandingPoints != null)
            {
                foreach (StandingPoint standingPoint in this.StandingPoints)
                {
                    if (standingPoint.GameEntity.HasTag("rotate"))
                    {
                        if (standingPoint.GameEntity.HasTag("left") && this._rotateStandingPoints.Count > 0)
                        {
                            this._rotateStandingPoints.Insert(0, standingPoint);
                        }
                        else
                        {
                            this._rotateStandingPoints.Add(standingPoint);
                        }
                    }
                }
                MatrixFrame globalFrame = this._body.GameEntity.GetGlobalFrame();
                this._standingPointLocalIKFrames = new MatrixFrame[this.StandingPoints.Count];
                for (int i = 0; i < this.StandingPoints.Count; i++)
                {
                    this._standingPointLocalIKFrames[i] = this.StandingPoints[i].GameEntity.GetGlobalFrame().TransformToLocal(globalFrame);
                    this.StandingPoints[i].AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
                }
            }
            this._missileBoneIndex = Skeleton.GetBoneIndexFromName(this.SkeletonOwnerObjects[0].GameEntity.Skeleton.GetName(), this._missileBoneName);
            this.ApplyAimChange();
            foreach (StandingPoint standingPoint2 in this.ReloadStandingPoints)
            {
                if (standingPoint2 != base.PilotStandingPoint)
                {
                    this._reloadWithoutPilot = standingPoint2;
                }
            }
            if (!GameNetwork.IsClientOrReplay)
            {
            }
            this.EnemyRangeToStopUsing = 13f;
            base.SetScriptComponentToTick(this.GetTickRequirement());

        }

        protected override void OnEditorInit()
        {
        }

        public Agent GetPilotAgent()
        {
            StandingPoint pilotStandingPoint = this.moverStandingPoint;
            if (pilotStandingPoint == null)
            {
                return null;
            }
            return null;
        }
        public Agent GetRotationAgent()
        {
            StandingPoint pilot = base.PilotStandingPoint;
            if (pilot == null)
            {
                return null;
            }
            return pilot.UserAgent;
        }

        protected override bool CanRotate()
        {
            return State == NWF_Cannon_StaticAngle.WeaponState.Idle || State == NWF_Cannon_StaticAngle.WeaponState.LoadingAmmo || State == NWF_Cannon_StaticAngle.WeaponState.WaitingBeforeIdle;
        }

        public override ScriptComponentBehavior.TickRequirement GetTickRequirement()
        {
            if (base.GameEntity.IsVisibleIncludeParents())
            {
                return base.GetTickRequirement() | ScriptComponentBehavior.TickRequirement.Tick | ScriptComponentBehavior.TickRequirement.TickParallel;
            }
            return base.GetTickRequirement();
        }

        protected void MoveControl()
        {
            if (GameNetwork.IsServer)
            {
                if (this.GetRotationAgent() != null)
                {
                    if (this.GetRotationAgent().Position.Distance(base.GameEntity.GlobalPosition) > 3f)
                    {
                        this.GetRotationAgent().StopUsingGameObjectMT(false);
                    }
                }
            }

            if (GameNetwork.IsClient)
            {
                if (Agent.Main != null && this.GetRotationAgent() == Agent.Main)
                {
                    float yaw = GetSignedYawOffset();
                    if (Mission.Current.InputManager.IsKeyPressed(InputKey.A))
                    {
                        if (yaw > -_maxYawRad) // Turning left is allowed until you exceed left bound
                            this.RequestTurningLeft();
                        else
                        {
                            this.RequestStopTurningLeft();
                        }
                    }
                    else if (Mission.Current.InputManager.IsKeyReleased(InputKey.A))
                    {
                        this.RequestStopTurningLeft();
                    }
                    if (Mission.Current.InputManager.IsKeyPressed(InputKey.D))
                    {
                        if (yaw < _maxYawRad) // Turning right is allowed until you exceed right bound
                            this.RequestTurningRight();
                        else
                        {
                            this.RequestStopTurningRight();
                        }
                    }
                    else if (Mission.Current.InputManager.IsKeyReleased(InputKey.D))
                    {
                        this.RequestStopTurningRight();
                    }
                    if (Mission.Current.InputManager.IsKeyPressed(InputKey.F))
                    {
                        GameNetwork.MyPeer.ControlledAgent.HandleStopUsingAction();
                        ActionIndexCache ac = ActionIndexCache.act_none;
                        this.GetRotationAgent().SetActionChannel(0, ac, true, 0UL, 0.0f, 1f, -0.2f, 0.4f, 0, false, -0.2f, 0, true);
                    }
                    if (yaw >= _maxYawRad)
                    {
                        this.RequestStopTurningRight();
                    }
                    if (yaw <= -_maxYawRad)
                    {
                        this.RequestStopTurningLeft();
                    }
                }
            }
            if (GameNetwork.IsServer)
            {

                if (this.GetRotationAgent() == null)
                {
                    float yaw = GetSignedYawOffset();

                    if (this.IsTurningLeft && yaw <= -_maxYawRad)
                    {
                        this.StopTurningLeft();
                    }
                    if (this.IsTurningRight && yaw >= _maxYawRad)
                    {
                        this.StopTurningRight();
                    }
                    if (this.IsTurningLeft) this.StopTurningLeft();
                    if (this.IsTurningRight) this.StopTurningRight();
                }
            }

            if (GameNetwork.IsClient)
            {

                float yaw = GetSignedYawOffset();

                if (this.IsTurningLeft && yaw <= -_maxYawRad)
                {
                    this.StopTurningLeft();
                }
                if (this.IsTurningRight && yaw >= _maxYawRad)
                {
                    this.StopTurningRight();
                }
                if (this.GetRotationAgent() == null)
                {
                    if (this.IsTurningLeft) this.StopTurningLeft();
                    if (this.IsTurningRight) this.StopTurningRight();
                }
            }
        }
        private const float _maxYawRad = MathF.PI * 0.25f;
        private float GetSignedYawOffset()
        {
            Vec2 startF = starterPosition.rotation.f.AsVec2.Normalized();
            Vec2 currF = GameEntity.GetFrame().rotation.f.AsVec2.Normalized();

            float cos = MBMath.ClampFloat(Vec2.DotProduct(startF, currF), -1f, 1f);
            float angle = MathF.Acos(cos);

            float sign = CrossProduct2D(currF, startF) < 0f ? -1f : 1f; // flipped order
            return angle * sign;
        }
        private float CrossProduct2D(Vec2 a, Vec2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
        protected override void OnTick(float dt)
        {
            base.OnTick(dt);
            this.MoveControl();

            if (GameNetwork.IsServer)
            {
                MatrixFrame frame = this.MoveObjectTick(dt);
                base.SetFrameSynched(ref frame);
            }


            if (!base.GameEntity.IsVisibleIncludeParents())
            {
                return;
            }
            if (!GameNetwork.IsClientOrReplay)
            {
                foreach (StandingPointWithWeaponRequirement standingPointWithWeaponRequirement in this.AmmoPickUpStandingPoints)
                {
                    if (standingPointWithWeaponRequirement.HasUser)
                    {
                        Agent userAgent = standingPointWithWeaponRequirement.UserAgent;
                        ActionIndexCache currentAction = userAgent.GetCurrentAction(1);
                        if (!(currentAction == NWF_Cannon_StaticAngle.act_pickup_boulder_begin))
                        {
                            if (currentAction == NWF_Cannon_StaticAngle.act_pickup_boulder_end)
                            {
                                MissionWeapon missionWeapon = new MissionWeapon(this.OriginalMissileItem, null, null, 1);
                                userAgent.EquipWeaponToExtraSlotAndWield(ref missionWeapon);
                                userAgent.StopUsingGameObject(true);
                                this.ConsumeAmmo();
                                if (userAgent.IsAIControlled)
                                {
                                    return;
                                }
                            }
                            else if (!userAgent.SetActionChannel(1, NWF_Cannon_StaticAngle.act_pickup_boulder_begin, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true) && userAgent.Controller != Agent.ControllerType.AI)
                            {
                                userAgent.StopUsingGameObject(true);
                            }
                        }
                    }
                }
                if (base.PilotAgent != null && !base.PilotAgent.IsInBeingStruckAction)
                {
                    if (base.PilotAgent.MovementFlags.HasAnyFlag(Agent.MovementControlFlag.AttackMask))
                    {
                        if (State == WeaponState.WaitingBeforeIdle)
                        {
                            if (!base.PilotAgent.IsAIControlled)
                            {
                                if (_timeElapsedAfterMessage == 0f)
                                {
                                    NetworkCommunicator peer = base.PilotAgent.MissionPeer.GetNetworkPeer();
                                    string message = $"Cannon requires to be struck by a ramrod to be able to shoot";
                                    SendMessageToPeer(message, peer);
                                    _timeElapsedAfterMessage = 1f;
                                }
                            }
                        }
                    }
                    if (!base.PilotAgent.MovementFlags.HasAnyFlag(Agent.MovementControlFlag.AttackMask))
                    {
                        if (State == WeaponState.WaitingBeforeIdle)
                        {
                            if (!base.PilotAgent.IsAIControlled)
                            {
                                _timeElapsedAfterMessage = 0f;
                            }
                        }
                    }
                }
            }
            switch (base.State)
            {
                case RangedSiegeWeapon.WeaponState.LoadingAmmo:
                    if (!GameNetwork.IsClientOrReplay)
                    {
                        if (this.LoadAmmoStandingPoint.HasUser)
                        {
                            Agent userAgent2 = this.LoadAmmoStandingPoint.UserAgent;
                            if (userAgent2.GetCurrentAction(1) == this._loadAmmoEndAnimationActionIndex)
                            {
                                EquipmentIndex wieldedItemIndex = userAgent2.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                                Debug.Print(wieldedItemIndex.ToString());
                                if (wieldedItemIndex != EquipmentIndex.None && userAgent2.Equipment[wieldedItemIndex].CurrentUsageItem.IsConsumable && userAgent2.Equipment[wieldedItemIndex].CurrentUsageItem.IsRangedWeapon)
                                {
                                    base.ChangeProjectileEntityServer(userAgent2, userAgent2.Equipment[wieldedItemIndex].Item.StringId);
                                    userAgent2.RemoveEquippedWeapon(wieldedItemIndex);
                                    this._timeElapsedAfterLoading = 0f;
                                    base.Projectile.SetVisibleSynched(true, false);
                                    base.State = RangedSiegeWeapon.WeaponState.WaitingBeforeIdle;
                                    return;
                                }
                                userAgent2.StopUsingGameObject(true);
                                if (!userAgent2.IsPlayerControlled)
                                {
                                    base.SendAgentToAmmoPickup(userAgent2);
                                    return;
                                }
                            }
                            else if (userAgent2.GetCurrentAction(1) != this._loadAmmoBeginAnimationActionIndex && !userAgent2.SetActionChannel(1, this._loadAmmoBeginAnimationActionIndex, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true))
                            {
                                for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
                                {
                                    if (!userAgent2.Equipment[equipmentIndex].IsEmpty && userAgent2.Equipment[equipmentIndex].CurrentUsageItem.IsConsumable && userAgent2.Equipment[equipmentIndex].CurrentUsageItem.IsRangedWeapon)
                                    {
                                        userAgent2.RemoveEquippedWeapon(equipmentIndex);
                                    }
                                }
                                userAgent2.StopUsingGameObject(true);
                                if (!userAgent2.IsPlayerControlled)
                                {
                                    base.SendAgentToAmmoPickup(userAgent2);
                                    return;
                                }
                            }
                        }
                        else if (this.LoadAmmoStandingPoint.HasAIMovingTo)
                        {
                            Agent movingAgent = this.LoadAmmoStandingPoint.MovingAgent;
                            EquipmentIndex wieldedItemIndex2 = movingAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                            if (wieldedItemIndex2 == EquipmentIndex.None || !movingAgent.Equipment[wieldedItemIndex2].CurrentUsageItem.IsConsumable || !movingAgent.Equipment[wieldedItemIndex2].CurrentUsageItem.IsRangedWeapon)
                            {
                                movingAgent.StopUsingGameObject(true);
                                base.SendAgentToAmmoPickup(movingAgent);
                            }
                        }
                    }
                    break;
                case RangedSiegeWeapon.WeaponState.WaitingBeforeIdle:
                    this._timeElapsedAfterLoading += dt;
                    if (this._ramrodhittime >= _ramrodhitlimit)
                    {
                        base.State = RangedSiegeWeapon.WeaponState.Idle;
                        _ramrodhittime = 0;
                        return;
                    }
                    break;
                case RangedSiegeWeapon.WeaponState.Reloading:
                case RangedSiegeWeapon.WeaponState.ReloadingPaused:
                    break;
                default:
                    return;
            }
        }
        protected override void ApplyCurrentDirectionToEntity()
        {

        }

        protected override void OnTickParallel(float dt)
        {
            base.OnTickParallel(dt);
            if (!base.GameEntity.IsVisibleIncludeParents())
            {
                return;
            }
            if (State == NWF_Cannon_StaticAngle.WeaponState.WaitingBeforeProjectileLeaving)
            {
                this.UpdateProjectilePosition();
            }
            if (this._verticalAdjuster.Skeleton != null)
            {
                float parameter = MBMath.ClampFloat((this.currentReleaseAngle - this.BottomReleaseAngleRestriction) / (this.TopReleaseAngleRestriction - this.BottomReleaseAngleRestriction), 0f, 1f);
                this._verticalAdjuster.Skeleton.SetAnimationParameterAtChannel(0, parameter);
            }
            MatrixFrame matrixFrame = this.SkeletonOwnerObjects[0].GameEntity.GetBoneEntitialFrameWithIndex(0).TransformToParent(this._verticalAdjusterStartingLocalFrame);
            this._verticalAdjuster.SetFrame(ref matrixFrame);
            MatrixFrame globalFrame = this._body.GameEntity.GetGlobalFrame();
            for (int i = 0; i < this.StandingPoints.Count; i++)
            {
                if (this.StandingPoints[i].HasUser)
                {
                    if (this.StandingPoints[i].UserAgent.IsInBeingStruckAction)
                    {
                        this.StandingPoints[i].UserAgent.ClearHandInverseKinematics();
                    }
                    else if (this.StandingPoints[i] != base.PilotStandingPoint)
                    {
                        if (this.StandingPoints[i].UserAgent.GetCurrentAction(1) != this._reload2IdleActionIndex)
                        {
                            this.StandingPoints[i].UserAgent.SetHandInverseKinematicsFrameForMissionObjectUsage(this._standingPointLocalIKFrames[i], globalFrame, 0f);
                        }
                        else
                        {
                            this.StandingPoints[i].UserAgent.ClearHandInverseKinematics();
                        }
                    }
                    else
                    {
                        this.StandingPoints[i].UserAgent.SetHandInverseKinematicsFrameForMissionObjectUsage(this._standingPointLocalIKFrames[i], globalFrame, 0f);
                    }
                }
            }
            if (!GameNetwork.IsClientOrReplay)
            {
                for (int j = 0; j < this._rotateStandingPoints.Count; j++)
                {
                    StandingPoint standingPoint = this._rotateStandingPoints[j];
                    if (standingPoint.HasUser && !standingPoint.UserAgent.SetActionChannel(1, (j == 0) ? this._rotateLeftAnimationActionIndex : this._rotateRightAnimationActionIndex, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true) && standingPoint.UserAgent.Controller != Agent.ControllerType.AI)
                    {
                        standingPoint.UserAgent.StopUsingGameObjectMT(true);
                    }
                }
                if (base.PilotAgent != null)
                {
                    ActionIndexCache currentAction = base.PilotAgent.GetCurrentAction(1);
                    if (State == NWF_Cannon_StaticAngle.WeaponState.WaitingBeforeProjectileLeaving)
                    {
                        if (base.PilotAgent.IsInBeingStruckAction)
                        {
                            if (currentAction != ActionIndexCache.act_none && currentAction != NWF_Cannon_StaticAngle.act_strike_bent_over)
                            {
                                base.PilotAgent.SetActionChannel(1, NWF_Cannon_StaticAngle.act_strike_bent_over, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
                            }
                        }
                        else if (!base.PilotAgent.SetActionChannel(1, this._shootAnimationActionIndex, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true) && base.PilotAgent.Controller != Agent.ControllerType.AI)
                        {
                            base.PilotAgent.StopUsingGameObjectMT(true);
                        }
                    }
                    else if (!base.PilotAgent.SetActionChannel(1, this._idleAnimationActionIndex, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true) && currentAction != this._reload1AnimationActionIndex && currentAction != this._shootAnimationActionIndex && base.PilotAgent.Controller != Agent.ControllerType.AI)
                    {
                        base.PilotAgent.StopUsingGameObjectMT(true);
                    }
                }
                if (this._reloadWithoutPilot.HasUser)
                {
                    Agent userAgent = this._reloadWithoutPilot.UserAgent;
                    if (!userAgent.SetActionChannel(1, this._reload2IdleActionIndex, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true) && userAgent.GetCurrentAction(1) != this._reload2AnimationActionIndex && userAgent.Controller != Agent.ControllerType.AI)
                    {
                        userAgent.StopUsingGameObjectMT(true);
                    }
                }
            }
            if (State == NWF_Cannon_StaticAngle.WeaponState.Reloading)
            {
                foreach (StandingPoint standingPoint2 in this.ReloadStandingPoints)
                {
                    if (standingPoint2.HasUser)
                    {
                        ActionIndexCache currentAction2 = standingPoint2.UserAgent.GetCurrentAction(1);
                        if (currentAction2 == this._reload1AnimationActionIndex || currentAction2 == this._reload2AnimationActionIndex)
                        {
                            standingPoint2.UserAgent.SetCurrentActionProgress(1, this._body.GameEntity.Skeleton.GetAnimationParameterAtChannel(0));
                        }
                        else if (!GameNetwork.IsClientOrReplay)
                        {
                            ActionIndexCache actionIndexCache = (standingPoint2 == base.PilotStandingPoint) ? this._reload1AnimationActionIndex : this._reload2AnimationActionIndex;
                            if (!standingPoint2.UserAgent.SetActionChannel(1, actionIndexCache, false, 0UL, 0f, 1f, -0.2f, 0.4f, this._body.GameEntity.Skeleton.GetAnimationParameterAtChannel(0), false, -0.2f, 0, true) && standingPoint2.UserAgent.Controller != Agent.ControllerType.AI)
                            {
                                standingPoint2.UserAgent.StopUsingGameObjectMT(true);
                            }
                        }
                    }
                }
            }
        }

        protected override void SetActivationLoadAmmoPoint(bool activate)
        {
            this.LoadAmmoStandingPoint.SetIsDeactivatedSynched(!activate);
        }

        protected override void UpdateProjectilePosition()
        {
            MatrixFrame boneEntitialFrameWithIndex = this.SkeletonOwnerObjects[0].GameEntity.GetBoneEntitialFrameWithIndex(this._missileBoneIndex);
            base.Projectile.GameEntity.SetFrame(ref boneEntitialFrameWithIndex);
        }

        protected override void OnRangedSiegeWeaponStateChange()
        {
            base.OnRangedSiegeWeaponStateChange();

            NWF_Cannon_StaticAngle.WeaponState state = State;
            if (state == NWF_Cannon_StaticAngle.WeaponState.Shooting)
            {
                ShootCanisterProjectile();


                Mat3 identity = Mat3.Identity;
                identity.f = GetBallisticErrorAppliedDirection(MaximumBallisticError);
                identity.Orthonormalize();
                Vec3 origin = Projectile.GameEntity.GetGlobalFrame().origin;
                MatrixFrame frame = new MatrixFrame(identity, origin);

                if (LoadedMissileItem.StringId == "nwf_artillery_shell_canister")
                {
                    Mission.Current.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName("musket_effect"), frame);
                }
                if (LoadedMissileItem.StringId == "nwf_artillery_shell_cannonball_6pd" || LoadedMissileItem.StringId == "nwf_artillery_shell_cannonball_12pd" || LoadedMissileItem.StringId == "nwf_artillery_shell_cannonball_24pd" || LoadedMissileItem.StringId == "nwf_artillery_shell_mortar" || LoadedMissileItem.StringId == "nwf_artillery_shell_howitzer")
                {
                    Mission.Current.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName("cannon_effect"), frame);
                }
            }

            if (state != NWF_Cannon_StaticAngle.WeaponState.Idle)
            {
                if (state != NWF_Cannon_StaticAngle.WeaponState.Shooting)
                {
                    if (state == NWF_Cannon_StaticAngle.WeaponState.WaitingBeforeIdle)
                    {
                        this.UpdateProjectilePosition();
                        return;
                    }
                }
                else
                {
                    if (!GameNetwork.IsClientOrReplay)
                    {
                        base.Projectile.SetVisibleSynched(false, false);
                        return;
                    }
                    base.Projectile.GameEntity.SetVisibilityExcludeParents(false);
                    return;
                }
            }
            else
            {
                if (!GameNetwork.IsClientOrReplay)
                {
                    base.Projectile.SetVisibleSynched(true, false);
                    return;
                }
                base.Projectile.GameEntity.SetVisibilityExcludeParents(true);
            }
        }

        protected override void GetSoundEventIndices()
        {
            this.MoveSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/mangonel/move");
            this.ReloadSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/mangonel/reload");
        }

        protected override float HorizontalAimSensitivity
        {
            get
            {
                if (this._defaultSide == BattleSideEnum.Defender)
                {
                    return 0.25f;
                }
                return 0.05f + (from rotateStandingPoint in this._rotateStandingPoints
                                where rotateStandingPoint.HasUser && !rotateStandingPoint.UserAgent.IsInBeingStruckAction
                                select rotateStandingPoint).Sum((StandingPoint rotateStandingPoint) => 0.1f);
            }
        }

        protected override float VerticalAimSensitivity
        {
            get
            {
                return 0.1f;
            }
        }

        protected override Vec3 ShootingDirection
        {
            get
            {
                Mat3 rotation = this._body.GameEntity.GetGlobalFrame().rotation;
                rotation.RotateAboutSide(-this.currentReleaseAngle);
                return rotation.TransformToParent(new Vec3(0f, -1f, 0f, -1f));
            }
        }



        protected override bool HasAmmo
        {
            get
            {
                return base.HasAmmo || base.CurrentlyUsedAmmoPickUpPoint != null || this.LoadAmmoStandingPoint.HasUser || this.LoadAmmoStandingPoint.HasAIMovingTo;
            }
            set
            {
                base.HasAmmo = value;
            }
        }

        public bool IsMovingForward { get; set; }
        public bool IsMovingBackward { get; set; }
        public bool IsTurningRight { get; set; }
        public bool IsTurningLeft { get; set; }
        public bool IsMovingUp { get; set; }
        public bool IsMovingDown { get; set; }
        public bool DestroyedByStoneOnly = false;
        public float HitPoint;
        public float MaxHitPoint = 200f;
        public string ParticleEffectOnDestroy = "psys_siege_sturgia_wall_destruction";

        public string SoundEffectOnDestroy = "event:/mission/siege/generic/stone_destroy";

        protected override void ApplyAimChange()
        {
            base.ApplyAimChange();
            this.ShootingDirection.Normalize();
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            if (!gameEntity.HasTag(this.AmmoPickUpTag))
            {
                return new TextObject(CannonName, null).ToString();
            }
            return new TextObject("Cannonball", null).ToString();
        }

        public override TextObject
            GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            TextObject textObject;
            if (usableGameObject.GameEntity.HasTag("reload"))
            {
                textObject = new TextObject((base.PilotStandingPoint == usableGameObject) ? "{=fEQAPJ2e}{KEY} Use" : "{=Na81xuXn}{KEY} Rearm", null);
            }
            else if (usableGameObject.GameEntity.HasTag("rotate"))
            {
                textObject = new TextObject("{=5wx4BF5h}{KEY} Rotate", null);
            }
            else if (usableGameObject.GameEntity.HasTag(this.AmmoPickUpTag))
            {
                textObject = new TextObject("{=bNYm3K6b}{KEY} Pick Up", null);
            }
            else if (usableGameObject.GameEntity.HasTag("ammoload"))
            {
                textObject = new TextObject("{=ibC4xPoo}{KEY} Load Ammo", null);
            }
            else
            {
                textObject = new TextObject("{=fEQAPJ2e}{KEY} Use", null);
            }
            textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            return textObject;
        }

        public override TargetFlags GetTargetFlags()
        {
            TargetFlags targetFlags = TargetFlags.None;
            targetFlags |= TargetFlags.IsFlammable;
            targetFlags |= TargetFlags.IsSiegeEngine;
            targetFlags |= TargetFlags.IsAttacker;
            if (base.IsDestroyed || this.IsDeactivated)
            {
                targetFlags |= TargetFlags.NotAThreat;
            }
            if (this.Side == BattleSideEnum.Attacker && DebugSiegeBehavior.DebugDefendState == DebugSiegeBehavior.DebugStateDefender.DebugDefendersToMangonels)
            {
                targetFlags |= TargetFlags.DebugThreat;
            }
            if (this.Side == BattleSideEnum.Defender && DebugSiegeBehavior.DebugAttackState == DebugSiegeBehavior.DebugStateAttacker.DebugAttackersToMangonels)
            {
                targetFlags |= TargetFlags.DebugThreat;
            }
            return targetFlags;
        }

        public override float GetTargetValue(List<Vec3> weaponPos)
        {
            return 40f * base.GetUserMultiplierOfWeapon() * this.GetDistanceMultiplierOfWeapon(weaponPos[0]) * base.GetHitPointMultiplierOfWeapon();
        }

        public override float ProcessTargetValue(float baseValue, TargetFlags flags)
        {
            if (flags.HasAnyFlag(TargetFlags.NotAThreat))
            {
                return -1000f;
            }
            if (flags.HasAnyFlag(TargetFlags.IsSiegeEngine))
            {
                baseValue *= 1.5f;
            }
            if (flags.HasAnyFlag(TargetFlags.IsStructure))
            {
                baseValue *= 2.5f;
            }
            if (flags.HasAnyFlag(TargetFlags.IsSmall))
            {
                baseValue *= 0.5f;
            }
            if (flags.HasAnyFlag(TargetFlags.IsMoving))
            {
                baseValue *= 0.8f;
            }
            if (flags.HasAnyFlag(TargetFlags.DebugThreat))
            {
                baseValue *= 10000f;
            }
            return baseValue;
        }

        protected override float GetDetachmentWeightAux(BattleSideEnum side)
        {
            return base.GetDetachmentWeightAuxForExternalAmmoWeapons(side);
        }

        public void SetSpawnedFromSpawner()
        {
            this._spawnedFromSpawner = true;
        }

        public void OnSpawnedByPrefab(PE_PrefabSpawner spawner)
        {
            this._spawnedFromSpawner = true;
        }

        public UsableMachine GetAttachedObject()
        {
            return this;
        }

        public void SetFrameAfterTick(MatrixFrame frame)
        {
            this._setFrameAfterTick = frame;
            this._frameSetFlag = true;
        }

        public float GetAdvanceSpeed()
        {
            float num = 0f;
            return num;
        }

        public float GetRotationSpeed()
        {
            float num = 0.1f;
            foreach (CannonStandingPoint standingPoint in this._rotateStandingPoints)
            {
                if (standingPoint.HasUser && !standingPoint.UserAgent.IsInBeingStruckAction)
                {
                    num += 0.05f;
                }
            }
            return num;
        }

        public float GetElevationSpeed()
        {
            return 0f;
        }

        public bool GetCanAdvance()
        {
            return false;
        }

        public bool GetCanRotate()
        {
            return true;
        }

        public bool GetCanElevate()
        {
            return false;
        }

        public bool GetAlwaysAlignToTerritory()
        {
            return true;
        }

        private void ShootCanisterProjectile()
        {
            if (base.PilotAgent != null && base.PilotAgent.IsActive())
            {
                _lastShooterAgent = base.PilotAgent;
                if (LoadedMissileItem.StringId == "nwf_artillery_shell_canister")
                {
                    ItemObject @object = Game.Current.ObjectManager.GetObject<ItemObject>("nwf_artillery_shell_canister_shot");
                    for (int i = 0; i < 35; i++)
                    {
                        ShootCanisterProjectileAux(@object, randomizeMissileSpeed: true);
                    }
                }
            }
            _lastShooterAgent = null;
        }

        private void ShootCanisterProjectileAux(ItemObject missileItem, bool randomizeMissileSpeed)
        {
            Mat3 identity = Mat3.Identity;
            float num = ShootingSpeed;
            if (randomizeMissileSpeed)
            {
                num *= MBRandom.RandomFloatRanged(0.98f, 1.02f);
                identity.f = GetBallisticErrorAppliedDirection(8.5f);
                identity.Orthonormalize();
            }
            else
            {
                identity.f = GetBallisticErrorAppliedDirection(MaximumBallisticError);
                identity.Orthonormalize();
            }

            Mission.Current.AddCustomMissile(_lastShooterAgent, new MissionWeapon(missileItem, null, _lastShooterAgent.Origin?.Banner, 1), ProjectileEntityCurrentGlobalPosition, identity.f, identity, LoadedMissileItem.PrimaryWeapon.MissileSpeed, num, addRigidBody: false, this);
        }

        private Agent _lastShooterAgent;
        private Vec3 GetBallisticErrorAppliedDirection(float BallisticErrorAmount)
        {
            Mat3 mat = default(Mat3);
            mat.f = ShootingDirection;
            mat.u = Vec3.Up;
            Mat3 mat2 = mat;
            mat2.Orthonormalize();
            float a = MBRandom.RandomFloat * ((float)Math.PI * 2f);
            mat2.RotateAboutForward(a);
            float f = BallisticErrorAmount * MBRandom.RandomFloat;
            mat2.RotateAboutSide(f.ToRadians());
            return mat2.f;
        }



        private bool HasTagInPrefabOrChildren(GameEntity entity, string tag)
        {
            if (entity.HasTag(tag))
                return true;

            foreach (GameEntity child in entity.GetChildren())
            {
                if (HasTagInPrefabOrChildren(child, tag))
                    return true;
            }

            return false;
        }

        public void playMoveSound()
        {
            if (MoveSound == null || !MoveSound.IsValid)
            {
                MoveSound = SoundEvent.CreateEvent(MoveSoundIndex, base.Scene);
                MoveSound.PlayInPosition(RotationObject.GameEntity.GlobalPosition);
            }
            MoveSound.Stop();
            MoveSound = null;
        }
        public void stopMoveSound()
        {
            if (MoveSound != null)
            {
                MoveSound.Stop();
                MoveSound = null;
            }
        }

        public void SetHitPoint(float hitPoint, Vec3 impactDirection)
        {

            this.HitPoint = hitPoint;

            MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
            if (this.HitPoint > this.MaxHitPoint) this.HitPoint = this.MaxHitPoint;
            if (this.HitPoint < 0) this.HitPoint = 0;

            if (this.HitPoint == 0)
            {
                for (int i = 0; i < this.StandingPoints.Count; i++)
                {
                    if (this.StandingPoints[i].HasUser)
                    {
                        this.StandingPoints[i].UserAgent.StopUsingGameObjectMT(false);
                    }
                }
                if (this.ParticleEffectOnDestroy != "")
                {
                    Mission.Current.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName(this.ParticleEffectOnDestroy), globalFrame);
                }
                if (this.SoundEffectOnDestroy != "")
                {
                    Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(this.SoundEffectOnDestroy), globalFrame.origin, false, true, -1, -1);
                }

                base.GameEntity.Remove(0);
            }
        }


        protected override bool OnHit(Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage)
        {
            reportDamage = true;
            MissionWeapon missionWeapon = weapon;
            WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
            if (currentUsageItem != null)
            {
                if (weapon.Item.StringId == "nwf_artillery_ramrod" && State != WeaponState.WaitingBeforeIdle)
                {
                    reportDamage = false;
                    damage = 0;
                    if (attackerAgent.IsPlayerControlled)
                    {
                        NetworkCommunicator peer = attackerAgent.MissionPeer.GetNetworkPeer();
                        string message = $"Cannon is currently not reloaded or needing a Ramrod";
                        SendMessageToPeer(message, peer);
                    }
                }
                if (weapon.Item.StringId == "nwf_artillery_ramrod" && State == WeaponState.WaitingBeforeIdle && _timeElapsedAfterLoading < 1f)
                {
                    reportDamage = false;
                    damage = 0;
                    if (attackerAgent.IsPlayerControlled)
                    {
                        NetworkCommunicator peer = attackerAgent.MissionPeer.GetNetworkPeer();
                        string message = $"Please Wait a moment before striking again";
                        SendMessageToPeer(message, peer);
                    }

                }
                if (weapon.Item.StringId == "nwf_artillery_ramrod" && State == WeaponState.WaitingBeforeIdle && _timeElapsedAfterLoading >= 1f)
                {
                    reportDamage = false;
                    damage = 0;
                    _ramrodhittime++;
                    this._timeElapsedAfterLoading = 0f;
                    if (attackerAgent.IsPlayerControlled)
                    {
                        NetworkCommunicator peer = attackerAgent.MissionPeer.GetNetworkPeer();
                        string message = $"Ramrod hits: {_ramrodhittime}/{_ramrodhitlimit}";
                        SendMessageToPeer(message, peer);
                    }
                }
            }
            if (this.DestroyedByStoneOnly)
            {
                if (currentUsageItem == null || (currentUsageItem.WeaponClass != WeaponClass.Stone && currentUsageItem.WeaponClass != WeaponClass.Boulder) || !currentUsageItem.WeaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
                {
                    reportDamage = false;
                    damage = 0;
                }
            }
            if (impactDirection == null) impactDirection = Vec3.Zero;


            this.SetHitPoint(this.HitPoint - damage, impactDirection);

            return false;
        }



        private const string BodyTag = "body";

        private const string RopeTag = "rope";

        private const string RotateTag = "rotate";

        private const string LeftTag = "left";

        private const string VerticalAdjusterTag = "vertical_adjuster";

        private static readonly ActionIndexCache act_usage_mangonel_idle = ActionIndexCache.Create("act_usage_mangonel_idle");

        private static readonly ActionIndexCache act_usage_mangonel_load_ammo_begin = ActionIndexCache.Create("act_usage_mangonel_load_ammo_begin");

        private static readonly ActionIndexCache act_usage_mangonel_load_ammo_end = ActionIndexCache.Create("act_usage_mangonel_load_ammo_end");

        private static readonly ActionIndexCache act_pickup_boulder_begin = ActionIndexCache.Create("act_pickup_boulder_begin");

        private static readonly ActionIndexCache act_pickup_boulder_end = ActionIndexCache.Create("act_pickup_boulder_end");

        private static readonly ActionIndexCache act_usage_mangonel_reload = ActionIndexCache.Create("act_usage_mangonel_reload");

        private static readonly ActionIndexCache act_usage_mangonel_reload_2 = ActionIndexCache.Create("act_usage_mangonel_reload_2");

        private static readonly ActionIndexCache act_usage_mangonel_reload_2_idle = ActionIndexCache.Create("act_usage_mangonel_reload_2_idle");

        private static readonly ActionIndexCache act_usage_mangonel_rotate_left = ActionIndexCache.Create("act_usage_mangonel_rotate_left");

        private static readonly ActionIndexCache act_usage_mangonel_rotate_right = ActionIndexCache.Create("act_usage_mangonel_rotate_right");

        private static readonly ActionIndexCache act_usage_mangonel_shoot = ActionIndexCache.Create("act_usage_mangonel_shoot");

        private static readonly ActionIndexCache act_usage_mangonel_big_idle = ActionIndexCache.Create("act_usage_mangonel_big_idle");

        private static readonly ActionIndexCache act_usage_mangonel_big_shoot = ActionIndexCache.Create("act_usage_mangonel_big_shoot");

        private static readonly ActionIndexCache act_usage_mangonel_big_reload = ActionIndexCache.Create("act_usage_mangonel_big_reload");

        private static readonly ActionIndexCache act_usage_mangonel_big_load_ammo_begin = ActionIndexCache.Create("act_usage_mangonel_big_load_ammo_begin");

        private static readonly ActionIndexCache act_usage_mangonel_big_load_ammo_end = ActionIndexCache.Create("act_usage_mangonel_big_load_ammo_end");

        private static readonly ActionIndexCache act_strike_bent_over = ActionIndexCache.Create("act_strike_bent_over");

        private string _missileBoneName = "end_throwarm";

        private List<StandingPoint> _rotateStandingPoints;

        private SynchedMissionObject _body;

        private SynchedMissionObject _rope;

        private GameEntity _verticalAdjuster;

        private MatrixFrame _verticalAdjusterStartingLocalFrame;

        private float _timeElapsedAfterLoading;

        private float _timeElapsedAfterMessage;

        private MatrixFrame[] _standingPointLocalIKFrames;

        private float _lastSyncedDirection;

        private float _lastSyncedReleaseAngle;

        private bool hasFrameChangedInPreviousFrame;
        private float _syncTimer;
        private StandingPoint _reloadWithoutPilot;
        private StandingPoint moverStandingPoint;
        public string MoverStandingPointTag = "mover";
        public string MangonelBodySkeleton = "mangonel_skeleton";

        public string MangonelBodyFire = "mangonel_fire";

        public string MangonelBodyReload = "mangonel_set_up";

        public string MangonelRopeFire = "mangonel_holder_fire";

        public string MangonelRopeReload = "mangonel_holder_set_up";

        public string MangonelAimAnimation = "mangonel_a_anglearm_state";

        public string ProjectileBoneName = "end_throwarm";

        public string IdleActionName;

        public string ShootActionName;

        public string Reload1ActionName;

        public string Reload2ActionName;

        public string RotateLeftActionName;

        public string RotateRightActionName;

        public string LoadAmmoBeginActionName;

        public string LoadAmmoEndActionName;

        public string Reload2IdleActionName;

        public string CannonName = "12-Pounder Cannon";

        private int _ramrodhittime;

        private int _ramrodhitlimit = 4;

        public float ProjectileSpeed = 40f;

        private ActionIndexCache _idleAnimationActionIndex;

        private ActionIndexCache _shootAnimationActionIndex;

        private ActionIndexCache _reload1AnimationActionIndex;

        private ActionIndexCache _reload2AnimationActionIndex;

        private ActionIndexCache _rotateLeftAnimationActionIndex;

        private ActionIndexCache _rotateRightAnimationActionIndex;

        private ActionIndexCache _loadAmmoBeginAnimationActionIndex;

        private ActionIndexCache _loadAmmoEndAnimationActionIndex;

        private ActionIndexCache _reload2IdleActionIndex;

        private sbyte _missileBoneIndex;
        private bool _frameSetFlag;
        private MatrixFrame _setFrameAfterTick;
    }
}