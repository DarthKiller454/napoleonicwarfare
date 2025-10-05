using Alliance.Common.Extensions.PE;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.PE
{
    public class PE_AttachToAgent : UsableMissionObject, IStray
    {
        public float HitPoint
        {
            get
            {
                return this._hitPoint;
            }
            set
            {
                bool flag = !this._hitPoint.Equals(value);
                if (flag)
                {
                    this._hitPoint = TaleWorlds.Library.MathF.Max(value, 0f);
                }
            }
        }

        public Agent AttachedTo { get; private set; }

        public override ScriptComponentBehavior.TickRequirement GetTickRequirement()
        {
            bool isClientOrReplay = GameNetwork.IsClientOrReplay;
            ScriptComponentBehavior.TickRequirement result;
            if (isClientOrReplay)
            {
                result = (base.GetTickRequirement() | ScriptComponentBehavior.TickRequirement.Tick | ScriptComponentBehavior.TickRequirement.TickParallel);
            }
            else
            {
                bool flag = GameNetwork.IsServer && this.AttachedTo != null;
                if (flag)
                {
                    result = (base.GetTickRequirement() | ScriptComponentBehavior.TickRequirement.Tick | ScriptComponentBehavior.TickRequirement.TickParallel);
                }
                else
                {
                    result = base.GetTickRequirement();
                }
            }
            return result;
        }

        protected override void OnTick(float dt)
        {
            bool flag = this.AttachedTo == null;
            if (!flag)
            {
                bool flag2 = !this.AttachedTo.IsActive();
                if (flag2)
                {
                    this.DetachFromAgentAux();
                }
                else
                {
                    GameEntity parent = base.GameEntity.Parent;
                    MatrixFrame globalFrame = parent.GetGlobalFrame();
                    globalFrame.rotation = this.AttachedTo.Frame.rotation;
                    globalFrame.Rotate(4.712389f, Vec3.Up);
                    parent.SetGlobalFrame(globalFrame);
                    globalFrame = parent.GetGlobalFrame();
                    Vec3 origin = base.GameEntity.GetGlobalFrame().origin;
                    Vec3 position = this.AttachedTo.Position;
                    Vec3 v = position - origin;
                    globalFrame.origin += v;
                    Vec3 u = default(Vec3);
                    float num = 0f;
                    base.Scene.GetTerrainHeightAndNormal(globalFrame.origin.AsVec2, out num, out u);
                    bool flag3 = globalFrame.origin.z <= num;
                    if (flag3)
                    {
                        globalFrame.origin.z = num;
                        globalFrame.rotation.u = u;
                        globalFrame.rotation.Orthonormalize();
                    }
                    else
                    {
                        globalFrame.rotation.u = new Vec3(0f, 0f, 1f, -1f);
                        globalFrame.rotation.Orthonormalize();
                    }
                    parent.SetGlobalFrame(globalFrame);
                }
            }
        }

        public void ResetStrayDuration()
        {
            this.WillBeDeletedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)this.StrayDurationSeconds;
        }

        protected override void OnInit()
        {
            base.OnInit();
            this.ActionMessage = new TextObject("Attach Object", null);
            TextObject textObject = new TextObject("Press {KEY} To Attach", null);
            textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            this.DescriptionMessage = textObject;
            this.ResetStrayDuration();
            GameEntity parent = base.GameEntity.Parent;
            SynchedMissionObject firstScriptOfType = parent.GetFirstScriptOfType<SynchedMissionObject>();
            FieldInfo field = typeof(SynchedMissionObject).GetField("_initialSynchFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            SynchedMissionObject.SynchFlags synchFlags = (SynchedMissionObject.SynchFlags)field.GetValue(firstScriptOfType);
            synchFlags |= SynchedMissionObject.SynchFlags.SynchTransform;
            field.SetValue(firstScriptOfType, synchFlags);
            base.IsInstantUse = true;
            this.HitPoint = this.MaxHitPoint;
        }

        public override bool IsDisabledForAgent(Agent agent)
        {
            return base.IsDeactivated || (base.IsDisabledForPlayers && !agent.IsAIControlled) || !agent.IsOnLand();
        }

        public void SetAttachedAgent(Agent attachedTo)
        {
            this.AttachToAgentAux(attachedTo);
        }

        public override void OnUse(Agent userAgent)
        {
            base.OnUse(userAgent);
            Debug.Print("[USING LOG] AGENT USING " + base.GetType().Name, 0, Debug.DebugColor.White, 17592186044416UL);
            bool flag = this.AttachedTo == null;
            if (flag)
            {
                bool attachableToHorse = this.AttachableToHorse;
                if (attachableToHorse)
                {
                    bool flag2 = !userAgent.HasMount;
                    if (!flag2)
                    {
                        bool flag3 = this.AttachableHorseType != "" && userAgent.MountAgent.Monster.StringId != this.AttachableHorseType;
                        if (!flag3)
                        {
                            this.AttachToAgentAux(userAgent.MountAgent);
                        }
                    }
                }
                else
                {
                    bool hasMount = userAgent.HasMount;
                    if (!hasMount)
                    {
                        this.AttachToAgentAux(userAgent);
                    }
                }
            }
            else
            {
                bool flag4 = this.AttachedTo == userAgent || (userAgent.MountAgent != null && this.AttachedTo == userAgent.MountAgent);
                if (flag4)
                {
                    this.DetachFromAgentAux();
                }
            }
        }

        private void DetachFromAgentAux()
        {
            this.AttachedTo = null;
        }

        private void AttachToAgentAux(Agent attachableAgent)
        {
            this.ResetStrayDuration();
            this.AttachedTo = attachableAgent;
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            return "Attach";
        }

        public bool IsStray()
        {
            bool flag = this.AttachedTo != null;
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                bool flag2 = this.WillBeDeletedAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                result = flag2;
            }
            return result;
        }

        public void SetHitPoint(float hitPoint, Vec3 impactDirection)
        {
            this.HitPoint = hitPoint;
            MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
        }

        protected override bool OnHit(Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage)
        {
            reportDamage = true;
            MissionWeapon missionWeapon = weapon;
            WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
            this.SetHitPoint(this.HitPoint - (float)damage, impactDirection);
            return false;
        }

        public PE_AttachToAgent() : base(false)
        {
        }

        public bool AttachableToHorse = false;

        public string AttachableHorseType = "";

        public int StrayDurationSeconds = 7200;

        private long WillBeDeletedAt = 0L;

        public string ParticleEffectOnDestroy = "";

        public string SoundEffectOnDestroy = "";

        public float MaxHitPoint = 500f;

        protected float _hitPoint;
    }
}
