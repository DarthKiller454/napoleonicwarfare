using Alliance.Common.Extensions.PE;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Extensions.PE
{
    abstract public class PE_UsableFromDistance : UsableMissionObject
    {
        public float Distance = 4f;

        public bool IsUsable(Agent user)
        {
            float distance = base.GameEntity.GetGlobalFrame().origin.Distance(user.Position);
            return distance <= this.Distance;
        }
    }
    public class PE_AttachToAgent : PE_UsableFromDistance, IStray
    {
        private MatrixFrame _attachPointLocalFrame;
        private bool _hasAttachPointLocalFrame;
        public bool AttachableToHorse = false;
        public string AttachableHorseType = "";
        public int StrayDurationSeconds = 7200;
        private long WillBeDeletedAt = 0;
        public string ParticleEffectOnDestroy = "";
        public string SoundEffectOnDestroy = "";

        public float MaxHitPoint = 500f;
        protected float _hitPoint;

        public float HitPoint
        {
            get => this._hitPoint;
            set
            {
                if (!this._hitPoint.Equals(value))
                {
                    this._hitPoint = MathF.Max(value, 0f);
                }
            }
        }

        public Agent AttachedTo { get; private set; }

        public override ScriptComponentBehavior.TickRequirement GetTickRequirement()
        {
            if (this.AttachedTo != null)
            {
                return base.GetTickRequirement()
                    | ScriptComponentBehavior.TickRequirement.Tick
                    | ScriptComponentBehavior.TickRequirement.TickParallel;
            }

            return base.GetTickRequirement();
        }

        protected override void OnTick(float dt)
        {
            if (this.AttachedTo == null)
                return;

            if (!this.AttachedTo.IsActive())
            {
                this.DetachFromAgentAux();
                return;
            }

            UpdateAttachedTransform();
        }

        public void ResetStrayDuration()
        {
            this.WillBeDeletedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + this.StrayDurationSeconds;
        }

        protected override void OnInit()
        {
            base.OnInit();
            base.ActionMessage = new TextObject("Attach Object");
            TextObject descriptionMessage = new TextObject("Press {KEY} To Attach");
            descriptionMessage.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            base.DescriptionMessage = descriptionMessage;
            this.ResetStrayDuration();
            WeakGameEntity parentEntity = base.GameEntity.Parent;
            if (parentEntity.IsValid)
            {
                // This is the local offset of the attach-point child inside the cannon prefab.
                _attachPointLocalFrame = base.GameEntity.GetFrame();
                _hasAttachPointLocalFrame = true;

                SynchedMissionObject synchObject = parentEntity.GetFirstScriptOfType<SynchedMissionObject>();

                if (synchObject != null)
                {
                    FieldInfo prop = typeof(SynchedMissionObject).GetField(
                        "_initialSynchFlags",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (prop != null)
                    {
                        SynchedMissionObject.SynchFlags syncFlags =
                            (SynchedMissionObject.SynchFlags)prop.GetValue(synchObject);

                        syncFlags |= SynchedMissionObject.SynchFlags.SynchTransform;
                        prop.SetValue(synchObject, syncFlags);
                    }
                }
            }
            base.IsInstantUse = true;
            this.HitPoint = this.MaxHitPoint;
        }
        public override bool IsDisabledForAgent(Agent agent)
        {

            if (!agent.Character.StringId.Contains("artillery") || !agent.IsPlayerControlled)
            {
                return true;
            }
            if (!IsDeactivated && agent.MountAgent == null && (!IsDisabledForPlayers || agent.IsAIControlled))
            {
                return !agent.IsOnLand();
            }
            return false;
        }

        public void SetAttachedAgent(Agent attachedTo)
        {
            this.AttachToAgentAux(attachedTo);
        }

        public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
        {

            base.OnUse(userAgent, agentBoneIndex);

            if (userAgent == null)
                return;

            if (this.AttachedTo == null)
            {
                if (this.AttachableToHorse)
                {

                    if (!userAgent.HasMount || userAgent.MountAgent == null)
                        return;

                    string horseType = userAgent.MountAgent.Monster != null
                        ? userAgent.MountAgent.Monster.StringId
                        : "";

                    if (!string.IsNullOrEmpty(this.AttachableHorseType) && horseType != this.AttachableHorseType)
                        return;
                    this.AttachToAgentAux(userAgent.MountAgent);
                    return;
                }

                if (userAgent.HasMount)
                    return;
                this.AttachToAgentAux(userAgent);
                return;
            }
            this.DetachFromAgentAux();
        }

        private void DetachFromAgentAux()
        {
            IMoveable moveable = GetParentMoveable();

            WeakGameEntity parentEntity = base.GameEntity.Parent;
            MatrixFrame finalFrame = MatrixFrame.Identity;

            if (parentEntity.IsValid)
                finalFrame = parentEntity.GetGlobalFrame();

            if (moveable != null)
            {
                moveable.ResetStoredYawFromCurrentFrame();
                moveable.SetExternalFrameControl(false);

                if (GameNetwork.IsServer)
                {
                    moveable.SyncCurrentFrame();
                }
            }

            this.AttachedTo = null;
            this.SetScriptComponentToTick(this.GetTickRequirement());

            if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncDetachFromAgent(this, finalFrame));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        private void AttachToAgentAux(Agent attachableAgent)
        {
            if (attachableAgent == null)
                return;

            this.ResetStrayDuration();
            this.AttachedTo = attachableAgent;

            IMoveable moveable = GetParentMoveable();

            if (moveable != null)
            {
                moveable.SetExternalFrameControl(true);
            }

            UpdateAttachedTransform();

            this.SetScriptComponentToTick(this.GetTickRequirement());

            if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncAttachToAgent(this, attachableAgent));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }
        private void UpdateAttachedTransform()
        {
            if (this.AttachedTo == null)
                return;

            WeakGameEntity parentEntity = base.GameEntity.Parent;

            if (!parentEntity.IsValid)
                return;

            if (!_hasAttachPointLocalFrame)
            {
                _attachPointLocalFrame = base.GameEntity.GetFrame();
                _hasAttachPointLocalFrame = true;
            }

            MatrixFrame horseFrame = this.AttachedTo.Frame;
            MatrixFrame parentFrame = parentEntity.GetGlobalFrame();

            parentFrame.rotation = horseFrame.rotation;

            Vec3 localAttachOffset = _attachPointLocalFrame.origin;
            Vec3 rotatedAttachOffset = parentFrame.rotation.TransformToParent(localAttachOffset);

            // This is the important movement line.
            parentFrame.origin = horseFrame.origin - rotatedAttachOffset;

            Vec3 normalVector;
            float terrainZ;

            base.Scene.GetTerrainHeightAndNormal(
                parentFrame.origin.AsVec2,
                out terrainZ,
                out normalVector);

            parentFrame.origin.z = terrainZ;
            parentFrame.rotation.u = normalVector;
            parentFrame.rotation.Orthonormalize();

            parentEntity.SetGlobalFrame(parentFrame);
            if (GameNetwork.IsServer)
            {
                IMoveable moveable = GetParentMoveable();

                if (moveable != null)
                {
                    moveable.ResetStoredYawFromCurrentFrame();
                    moveable.SyncCurrentFrame();
                }
            }
        }
        public void SetDetachedFromServer(MatrixFrame finalFrame)
        {
            WeakGameEntity parentEntity = base.GameEntity.Parent;

            this.AttachedTo = null;

            IMoveable moveable = GetParentMoveable();

            if (moveable != null)
            {
                moveable.SetExternalFrameControl(false);
                moveable.ResetStoredYawFromCurrentFrame();
            }

            if (parentEntity.IsValid)
            {
                parentEntity.SetGlobalFrame(finalFrame);
            }

            this.SetScriptComponentToTick(this.GetTickRequirement());
        }
        private IMoveable GetParentMoveable()
        {
            WeakGameEntity parentEntity = base.GameEntity.Parent;

            if (!parentEntity.IsValid)
                return null;

            UsableMachine usableMachine = parentEntity.GetFirstScriptOfType<UsableMachine>();

            return usableMachine as IMoveable;
        }

        public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
        {
            return new TextObject("");
        }

        public bool IsStray()
        {
            if (this.AttachedTo != null) return false;
            if (this.WillBeDeletedAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return true;
            return false;
        }

        public void SetHitPoint(float hitPoint, Vec3 impactDirection)
        {
            this.HitPoint = hitPoint;
            MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
        }

        protected override bool OnHit(Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, int affectorWeaponSlotOrMissileIndex, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage, out float finalDamage, out float fireDamage, out float modifiedFireDamage)
        {
            reportDamage = false;
            damage = 0;
            finalDamage = (float)damage;
            fireDamage = 0f;
            modifiedFireDamage = 0f;
            return false;
        }
    }
}
