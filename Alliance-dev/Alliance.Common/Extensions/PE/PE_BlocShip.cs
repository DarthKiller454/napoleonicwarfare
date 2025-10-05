using Alliance.Common.Extensions.PE;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace PersistentEmpiresLib.SceneScripts
{

    public class PE_BlocShip : PE_MoveableMachine
    {
        // Credits to fucking Bloc
        public bool isPlayerUsing = false;
        public string Animation = "";
        public string ShipName = "Ship";
        public int StrayDurationSeconds = 600;
        public string ParticleEffectOnDestroy = "";
        public string SoundEffectOnDestroy = "";
        public float frontRearRadius = 4.5f;
        public float sideRadius = 2f;
        public bool DestroyedByStoneOnly = false;

        private long WillBeDeletedAt = 0;

        private bool destroyed = false;

        private readonly Dictionary<InputKey, Action> inputActions = new Dictionary<InputKey, Action>
        {
            { InputKey.W, null },
            { InputKey.S, null },
            { InputKey.A, null },
            { InputKey.D, null },
            { InputKey.Space, null },
            { InputKey.LeftShift, null }
        };

        public override ScriptComponentBehavior.TickRequirement GetTickRequirement() => !this.GameEntity.IsVisibleIncludeParents() ? base.GetTickRequirement() : ScriptComponentBehavior.TickRequirement.Tick | ScriptComponentBehavior.TickRequirement.TickParallel;


        private void CheckIfLanded(MatrixFrame oldFrame)
        {
            if (base.GameEntity?.GlobalPosition == null || oldFrame == null)
                return;

            // Set a Default Value for Rays TODO

            Vec3 startPosition = base.GameEntity.GlobalPosition;

            Vec3 movementDirection = GetMovementDirection();
            if (movementDirection == Vec3.Zero)
                return;

            float radius = frontRearRadius;
            if (movementDirection == -base.GameEntity.GetGlobalFrame().rotation.s || movementDirection == base.GameEntity.GetGlobalFrame().rotation.s)
                radius = sideRadius;

            Vec3 raycastPosition = startPosition;
            Vec3 raycastEndPosition = raycastPosition + movementDirection * radius;

            if (Mission.Current.Scene.RayCastForClosestEntityOrTerrain(raycastPosition, raycastEndPosition, out _, out GameEntity hitEntity))
            {
                if (hitEntity != base.GameEntity)
                {
                    StopShip();
                    GameEntity.SetGlobalFrame(oldFrame);
                    PlayCollisionEffects(startPosition);
                }
            }
        }

        private Vec3 GetMovementDirection()
        {
            MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
            switch (true)
            {
                case var _ when base.IsMovingForward:
                    return globalFrame.rotation.f;
                case var _ when base.IsMovingBackward:
                    return -globalFrame.rotation.f;
                case var _ when base.IsTurningLeft:
                    return -globalFrame.rotation.s;
                case var _ when base.IsTurningRight:
                    return globalFrame.rotation.s;
                default:
                    return Vec3.Zero;
            }
        }

        private void PlayCollisionEffects(Vec3 position)
        {
            Mission.Current.MakeSound(SoundEvent.GetEventIdFromString("event:/mission/siege/merlon/wood_destroy"), position, false, true, -1, -1);
            this.SetHitPoint(this.HitPoint - 10, Vec3.Zero);
        }

        protected override void OnTick(float dt)
        {
            if (base.GameEntity == null) return;

            MatrixFrame oldFrame = base.GameEntity.GetFrame();
            base.OnTick(dt);

            if (GameNetwork.IsServer)
            {
                if (this.PilotAgent != null)
                {
                    this.ResetStrayDuration();
                }
            }

            if (GameNetwork.IsClient && Agent.Main != null && this.PilotAgent == Agent.Main)
            {
                foreach (var inputAction in inputActions)
                {
                    inputAction.Value?.Invoke();
                }

                if (Mission.Current.InputManager.IsKeyPressed(InputKey.F))
                {
                    GameNetwork.MyPeer.ControlledAgent.HandleStopUsingAction();
                    this.isPlayerUsing = false;
                    ActionIndexCache ac = ActionIndexCache.act_none;
                    this.PilotAgent.SetActionChannel(0, ac, true, 0UL, 0.0f, 1f, -0.2f, 0.4f, 0, false, -0.2f, 0, true);
                }
            }

            if (this.PilotAgent == null)
            {
                StopShip();
            }

            if (GameNetwork.IsServer && (base.IsMovingBackward || base.IsMovingDown || base.IsMovingForward || base.IsMovingUp || base.IsTurningLeft || base.IsTurningRight || this.PilotAgent != null))
            {
                this.CheckIfLanded(oldFrame);
            }

            if (destroyed)
            {
                base.GameEntity.Remove(0);
            }
        }

        private void StopShip()
        {
            if (base.IsMovingBackward) this.StopMovingBackward();
            if (base.IsMovingDown) this.StopMovingDown();
            if (base.IsMovingForward) this.StopMovingForward();
            if (base.IsMovingUp) this.StopMovingUp();
            if (base.IsTurningLeft) this.StopTurningLeft();
            if (base.IsTurningRight) this.StopTurningRight();
        }


        protected override void OnTickParallel(float dt)
        {
            base.OnTickParallel(dt);
            if (!base.GameEntity.IsVisibleIncludeParents())
            {
                return;
            }
        }
        protected override void OnInit()
        {
            base.OnInit();

            inputActions[InputKey.W] = () => { if (Mission.Current.InputManager.IsKeyPressed(InputKey.W)) this.RequestMovingForward(); else if (Mission.Current.InputManager.IsKeyReleased(InputKey.W)) this.RequestStopMovingForward(); };
            inputActions[InputKey.S] = () => { if (Mission.Current.InputManager.IsKeyPressed(InputKey.S)) this.RequestMovingBackward(); else if (Mission.Current.InputManager.IsKeyReleased(InputKey.S)) this.RequestStopMovingBackward(); };
            inputActions[InputKey.A] = () => { if (Mission.Current.InputManager.IsKeyPressed(InputKey.A)) this.RequestTurningLeft(); else if (Mission.Current.InputManager.IsKeyReleased(InputKey.A)) this.RequestStopTurningLeft(); };
            inputActions[InputKey.D] = () => { if (Mission.Current.InputManager.IsKeyPressed(InputKey.D)) this.RequestTurningRight(); else if (Mission.Current.InputManager.IsKeyReleased(InputKey.D)) this.RequestStopTurningRight(); };
            inputActions[InputKey.Space] = () => { if (Mission.Current.InputManager.IsKeyPressed(InputKey.Space)) this.RequestMovingUp(); else if (Mission.Current.InputManager.IsKeyReleased(InputKey.Space)) this.RequestStopMovingUp(); };
            inputActions[InputKey.LeftShift] = () => { if (Mission.Current.InputManager.IsKeyPressed(InputKey.LeftShift)) this.RequestMovingDown(); else if (Mission.Current.InputManager.IsKeyReleased(InputKey.LeftShift)) this.RequestStopMovingDown(); };

            foreach (StandingPoint standingPoint in this.StandingPoints)
            {
                standingPoint.AutoSheathWeapons = true;
            }
            this.ResetStrayDuration();
            this.HitPoint = this.MaxHitPoint;
        }
        public bool IsAgentFullyUsing(Agent usingAgent)
        {
            return this.PilotAgent == usingAgent;
        }
        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            TextObject forStandingPoint = new TextObject(this.IsAgentFullyUsing(GameNetwork.MyPeer.ControlledAgent) ? "{=QGdaakYW}{KEY} Stop Using" : "{=bl2aRW8f}{KEY} Command Ship");
            forStandingPoint.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            return forStandingPoint;
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            return new TextObject(ShipName).ToString();
        }

        public override bool IsStray()
        {
            if (this.PilotAgent != null) return false;
            return this.WillBeDeletedAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public override void ResetStrayDuration()
        {
            this.WillBeDeletedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + this.StrayDurationSeconds;
        }

        public override void SetHitPoint(float hitPoint, Vec3 impactDirection)
        {
            this.HitPoint = hitPoint;
            MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
            if (this.HitPoint > this.MaxHitPoint) this.HitPoint = this.MaxHitPoint;
            if (this.HitPoint < 0) this.HitPoint = 0;

            if (this.HitPoint == 0)
            {
                this.PilotAgent?.StopUsingGameObjectMT(false);
                if (!string.IsNullOrEmpty(this.ParticleEffectOnDestroy))
                {
                    Mission.Current.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName(this.ParticleEffectOnDestroy), globalFrame);
                }
                if (!string.IsNullOrEmpty(this.SoundEffectOnDestroy))
                {
                    Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(this.SoundEffectOnDestroy), globalFrame.origin, false, true, -1, -1);
                }
                destroyed = true;
            }
        }


        protected override bool OnHit(Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage)
        {
            reportDamage = true;
            WeaponComponentData currentUsageItem = weapon.CurrentUsageItem;

            if (this.DestroyedByStoneOnly && (currentUsageItem == null || (currentUsageItem.WeaponClass != WeaponClass.Stone && currentUsageItem.WeaponClass != WeaponClass.Boulder) || !currentUsageItem.WeaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand)))
            {
                damage = 0;
            }

            if (impactDirection == null)
                impactDirection = Vec3.Zero;
            this.SetHitPoint(this.HitPoint - damage, impactDirection);

            return false;
        }
    }
}