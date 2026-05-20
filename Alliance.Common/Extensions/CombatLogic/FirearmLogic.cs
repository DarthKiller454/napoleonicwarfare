using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Extensions.CombatLogic
{
    public class FirearmsMissionLogic : MissionLogic
    {
        private readonly HashSet<Agent> _lockedReloadAgents = new HashSet<Agent>();
        private sealed class VolleyGroupState
        {
            public float WaitStartedAt = -1f;
            public float OpenFireUntil = -1f;
            public bool HasEverUnlocked;
        }
        private const float VolleyMaxReloadWait = 15f;
        private const float VolleyOpenFireWindow = 5f;

        private readonly List<Agent> _volleyAgents = new List<Agent>(128);

        private readonly Dictionary<ValueTuple<Formation, WeaponClass?>, bool> _volleyGroupHasReloading =
            new Dictionary<ValueTuple<Formation, WeaponClass?>, bool>();

        private readonly Dictionary<ValueTuple<Formation, WeaponClass?>, VolleyGroupState> _volleyGroupStates =
            new Dictionary<ValueTuple<Formation, WeaponClass?>, VolleyGroupState>();

        private readonly HashSet<ValueTuple<Formation, WeaponClass?>> _activeVolleyGroups =
            new HashSet<ValueTuple<Formation, WeaponClass?>>();
        private static void SetUnitAttack(Agent agent)
        {
            agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanAttack);
        }

        private static void SetUnitNoAttack(Agent agent)
        {
            agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanAttack);
        }

        private static WeaponClass? GetAgentWeaponClass(Agent agent)
        {
            if (agent == null)
                return null;

            WeaponComponentData currentUsageItem = agent.WieldedWeapon.CurrentUsageItem;
            if (currentUsageItem == null)
                return null;

            return currentUsageItem.WeaponClass;
        }

        private static bool HasRangedWeapon(Agent agent)
        {
            if (agent == null)
                return false;

            WeaponComponentData currentUsageItem = agent.WieldedWeapon.CurrentUsageItem;
            if (currentUsageItem == null)
                return false;

            return currentUsageItem.IsRangedWeapon;
        }

        private static bool UnitCanShoot(Agent agent)
        {
            if (!HasRangedWeapon(agent))
                return false;

            return !agent.WieldedWeapon.IsReloading;
        }
        private static bool IsVolleyCandidate(Agent agent)
        {
            if (agent == null || !agent.IsActive() || !agent.IsHuman)
                return false;

            if (!agent.IsAIControlled)
                return false;

            if (agent.HasMount)
                return false;

            Formation formation = agent.Formation;
            if (formation == null)
                return false;

            if (formation.GetMovementState() == MovementOrder.MovementStateEnum.Charge)
                return false;

            if (!HasRangedWeapon(agent))
                return false;

            return true;
        }

        private void Volley()
        {
            float now = Mission.Current.CurrentTime;

            _volleyGroupHasReloading.Clear();
            _activeVolleyGroups.Clear();

            // First pass:
            // Determine which formation + weapon-class groups currently have reloaders.
            foreach (Agent agent in _volleyAgents)
            {
                if (agent == null || agent.Formation == null)
                    continue;

                ValueTuple<Formation, WeaponClass?> key =
                    new ValueTuple<Formation, WeaponClass?>(agent.Formation, GetAgentWeaponClass(agent));

                _activeVolleyGroups.Add(key);

                if (!_volleyGroupHasReloading.ContainsKey(key))
                    _volleyGroupHasReloading[key] = false;

                if (!UnitCanShoot(agent))
                    _volleyGroupHasReloading[key] = true;

                if (!_volleyGroupStates.ContainsKey(key))
                    _volleyGroupStates[key] = new VolleyGroupState();
            }

            // Remove old formation states when no unit currently uses them.
            List<ValueTuple<Formation, WeaponClass?>> keysToRemove = null;

            foreach (ValueTuple<Formation, WeaponClass?> key in _volleyGroupStates.Keys)
            {
                if (!_activeVolleyGroups.Contains(key))
                {
                    if (keysToRemove == null)
                        keysToRemove = new List<ValueTuple<Formation, WeaponClass?>>();

                    keysToRemove.Add(key);
                }
            }

            if (keysToRemove != null)
            {
                foreach (ValueTuple<Formation, WeaponClass?> key in keysToRemove)
                {
                    _volleyGroupStates.Remove(key);
                }
            }

            // Second pass:
            // Apply volley timing.
            foreach (Agent agent in _volleyAgents)
            {
                if (agent == null || agent.Formation == null)
                    continue;

                ValueTuple<Formation, WeaponClass?> key =
                    new ValueTuple<Formation, WeaponClass?>(agent.Formation, GetAgentWeaponClass(agent));

                VolleyGroupState state = _volleyGroupStates[key];

                bool groupHasReloading;
                _volleyGroupHasReloading.TryGetValue(key, out groupHasReloading);

                bool openFireWindowActive = now < state.OpenFireUntil;

                // During the open-fire window, never block attacks.
                // This prevents slow aimers from being cancelled immediately
                // when the first agents fire and enter reload.
                if (openFireWindowActive)
                {
                    SetUnitAttack(agent);
                    continue;
                }

                // Nobody is reloading:
                // The group is fully ready. Unlock volley and give them a 2s firing window.
                if (!groupHasReloading)
                {
                    bool wasWaitingForReload = state.WaitStartedAt >= 0f;

                    state.WaitStartedAt = -1f;

                    if (!state.HasEverUnlocked || wasWaitingForReload)
                    {
                        state.HasEverUnlocked = true;
                        state.OpenFireUntil = now + VolleyOpenFireWindow;
                    }

                    SetUnitAttack(agent);
                    continue;
                }

                // Someone is reloading:
                // Start or continue the wait timer.
                if (state.WaitStartedAt < 0f)
                    state.WaitStartedAt = now;

                bool reloadWaitTimedOut = now - state.WaitStartedAt >= VolleyMaxReloadWait;

                if (reloadWaitTimedOut)
                {
                    // Safety release:
                    // If reload is stuck/interrupted for too long, force another volley window.
                    state.WaitStartedAt = -1f;
                    state.OpenFireUntil = now + VolleyOpenFireWindow;
                    state.HasEverUnlocked = true;

                    SetUnitAttack(agent);
                    continue;
                }

                // Normal volley waiting behavior:
                // Reloading agents must be allowed to continue their reload.
                // Loaded agents are held back until the whole group is ready again.
                if (UnitCanShoot(agent))
                {
                    SetUnitNoAttack(agent);
                }
                else
                {
                    SetUnitAttack(agent);
                }
            }
        }
        public FirearmsMissionLogic()
        {
        }
        public override void OnMissionTick(float dt)
        {
            _volleyAgents.Clear();
            foreach (Agent agent in Mission.Current.Agents)
            {
                if (agent == null || !agent.IsActive() || !agent.IsHuman)
                    continue;

                bool shouldLockMovement = IsInReloadLockState(agent);
                if (agent.IsAIControlled)
                {
                    Agent targetAgent = agent.GetTargetAgent();

                    // Cancel AI Reload if enemy is too close
                    if (targetAgent != null && targetAgent.IsActive() && targetAgent.IsHuman && shouldLockMovement)
                    {
                        float distance = agent.GetDistanceTo(targetAgent);
                        if (distance < 5.5f)
                        {
                            agent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0xCuL);
                        }
                    }

                    // Crouch first rank
                    if (agent.Formation != null && !agent.HasMount && agent.Formation.GetMovementState() == MovementOrder.MovementStateEnum.Hold)
                    {
                        agent.GetFormationFileAndRankInfo(
                            out int fileIndex,
                            out int rankIndex,
                            out int fileCount,
                            out int rankCount);

                        Vec2 velocity = agent.GetCurrentVelocity();
                        ArrangementOrder arrangementOrder = agent.Formation.ArrangementOrder;

                            bool isLooseOrder = arrangementOrder == ArrangementOrder.ArrangementOrderLoose;

                        bool validRankForCrouch = isLooseOrder
                            ? rankIndex <= 1
                            : rankIndex == 0;

                        bool shouldCrouch =
                            rankCount >= 2 &&
                            validRankForCrouch &&
                            velocity == Vec2.Zero;

                        if (agent.CrouchMode != shouldCrouch)
                        {
                            agent.SetCrouchMode(shouldCrouch);
                        }
                    }
                    else
                    {
                        if (agent.CrouchMode)
                        {
                            agent.SetCrouchMode(false);
                        }
                    }
                    if (IsVolleyCandidate(agent))
                    {
                        _volleyAgents.Add(agent);
                    }
                    else
                    {
                        // Important:
                        // If this unit was blocked by volley before, restore normal attacking
                        // once it leaves the volley conditions.
                        SetUnitAttack(agent);
                    }
                }
                // Cancel Reload if player moves
                if (shouldLockMovement)
                {
                    if (!_lockedReloadAgents.Contains(agent))
                        _lockedReloadAgents.Add(agent);

                    if (agent.HasMount)
                    {
                        Agent mountAgent = agent.MountAgent;
                        Vec2 velocity = Vec2.Zero;
                        velocity = mountAgent.GetCurrentVelocity();
                        if (velocity != Vec2.Zero)
                        {
                            agent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0xCuL);
                        }
                    }
                    else
                    {
                        Vec2 agentvelocity = Vec2.Zero;
                        agentvelocity = agent.GetCurrentVelocity();
                        if (agentvelocity != Vec2.Zero)
                        {
                            agent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0xCuL);
                        }
                    }
                }
                else
                {
                    if (_lockedReloadAgents.Remove(agent))
                    {
                    }
                }
            }
            Volley();

            base.OnMissionTick(dt);
        }
        private static bool IsInReloadLockState(Agent agent)
        {
            ActionIndexCache currentAction = agent.GetCurrentAction(1);
            Agent.ActionStage currentStage = agent.GetCurrentActionStage(1);
            if (currentAction == ActionIndexCache.act_none
            || currentAction.Index < 0
            || string.IsNullOrEmpty(currentAction.GetName())
            || currentAction.GetName == ActionIndexCache.act_none.GetName
            || !currentStage.Equals(Agent.ActionStage.ReloadMidPhase))
            {
                return false;
            }
            return true;
        }

        public override void OnAgentRemoved(
            Agent affectedAgent,
            Agent affectorAgent,
            AgentState agentState,
            KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            _lockedReloadAgents.Remove(affectedAgent);
        }
        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);
            if (GameNetwork.IsClient)
                return;
            if (shooterAgent == null || !shooterAgent.IsActive())
                return;

            var wieldedWeapon = shooterAgent.WieldedWeapon;
            if (wieldedWeapon.IsEmpty)
                return;

            var usageItem = wieldedWeapon.CurrentUsageItem;
            if (usageItem == null)
                return;

            var ammoWeapon = wieldedWeapon.AmmoWeapon;
            if (ammoWeapon.IsEmpty || ammoWeapon.Item == null)
                return;

            var frame = new MatrixFrame(orientation, position);
            float maxAngle = MathF.PI / 8f;
            float randomAngle = MBRandom.RandomFloatRanged(-maxAngle, maxAngle);
            var offset = (shooterAgent.WieldedWeapon.CurrentUsageItem.WeaponLength + 5) / 100;
            frame.Advance(offset);
            frame.rotation.RotateAboutUp(randomAngle);
            

            string ammoId = ammoWeapon.Item.StringId;

            if (ammoId == "cartridges")
            {
                Mission.AddParticleSystemBurstByName("musket_effect", frame, true);
            }
            else if (ammoId == "pistol_cartridges")
            {
                Mission.AddParticleSystemBurstByName("pistol_effect", frame, true);
            }
        }

        public override void OnMissileCollisionReaction(
    Mission.MissileCollisionReaction collisionReaction,
    Agent attackerAgent,
    Agent attachedAgent,
    sbyte attachedBoneIndex)
        {
            base.OnMissileCollisionReaction(collisionReaction, attackerAgent, attachedAgent, attachedBoneIndex);
            if (GameNetwork.IsClient)
                return;
            var missile = Mission.MissilesList.FirstOrDefault(m => m.ShooterAgent == attackerAgent);
            if (missile == null)
                return;

            var item = missile.Weapon.Item;
            if (item == null)
                return;

            if (collisionReaction != Mission.MissileCollisionReaction.BecomeInvisible)
                return;

            if (item.StringId.Contains("cannonball"))
            {
            Vec3 velocity = missile.GetVelocity();
            float speed = velocity.Length;
            if (speed < 4f)
                return;

            Vec3 originalDir = velocity.NormalizedCopy();

            Vec3 missilePos = missile.Entity.GlobalPosition;
            float terrainZ;
            Vec3 terrainNormal;
            Mission.Current.Scene.GetTerrainHeightAndNormal(missilePos.AsVec2, out terrainZ, out terrainNormal);

            float heightDiff = missilePos.z - terrainZ;

            if (heightDiff > 1.0f || heightDiff < -0.5f)
                return;

            bool hitTerrain = missile.Entity?.Parent == null;
            if (!hitTerrain)
                return;

            Vec3 impactDirection = originalDir.NormalizedCopy();

            Vec3 sideDirection = Vec3.CrossProduct(impactDirection, terrainNormal).NormalizedCopy();

            Mat3 craterOrientation = Mat3.Identity;
            craterOrientation.f = impactDirection;
            craterOrientation.s = sideDirection;
            craterOrientation.u = terrainNormal;
            craterOrientation.Orthonormalize();

            Vec3 craterPos = new Vec3(missilePos.x, missilePos.y, terrainZ);

            SpawnCraterAt(craterPos, craterOrientation);

            Vec3 bounceDirection = ReflectVector(originalDir, terrainNormal);

            float steepness = TaleWorlds.Library.MathF.Abs(Vec3.DotProduct(originalDir, terrainNormal));

            float horizontalDamp = 1.0f;
            float verticalDamp = MBMath.Lerp(0.2f, 0.04f, steepness);

            bounceDirection = new Vec3(
                bounceDirection.x * horizontalDamp,
                bounceDirection.y * horizontalDamp,
                bounceDirection.z * verticalDamp
            ).NormalizedCopy();

            float dirDot = Vec3.DotProduct(bounceDirection, originalDir);
            if (dirDot < 0.3f)
                return;

            float speedMultiplier = MBMath.Lerp(0.95f, 0.4f, steepness);
            float newSpeed = speed * speedMultiplier;

            float verticalBiasStrength = MBMath.Lerp(0.0f, 0.68f, 1.0f - dirDot);
            bounceDirection.z -= verticalBiasStrength;
            bounceDirection = bounceDirection.NormalizedCopy();

            Mat3 orientation = Mat3.Identity;
            orientation.f = bounceDirection;
            orientation.Orthonormalize();

            Vec3 spawnPos = missilePos + terrainNormal * 0.07f;

            if (item.StringId.Equals("nwf_artillery_shell_cannonball_6pd"))
                item = Game.Current.ObjectManager.GetObject<ItemObject>("nwf_artillery_shell_cannonball_6pd_bounce");
            if (item.StringId.Equals("nwf_artillery_shell_cannonball_12pd"))
                item = Game.Current.ObjectManager.GetObject<ItemObject>("nwf_artillery_shell_cannonball_12pd_bounce");
            if (item.StringId.Equals("nwf_artillery_shell_cannonball_24pd"))
                item = Game.Current.ObjectManager.GetObject<ItemObject>("nwf_artillery_shell_cannonball_24pd_bounce");

            Mission.Current.AddCustomMissile(
                attackerAgent,
                new MissionWeapon(item, null, attackerAgent.Origin?.Banner, 1),
                spawnPos,
                bounceDirection,
                orientation,
                newSpeed,
                newSpeed,
                addRigidBody: false,
                missile.MissionObjectToIgnore
            );
            }


            if (item.StringId.Contains("nwf_artillery_shell_howitzer") || item.StringId.Contains("nwf_artillery_shell_mortar"))
            {

                Vec3 velocity = missile.GetVelocity();
                float speed = velocity.Length;
                Vec3 missilePos = missile.Entity.GlobalPosition;
                float terrainZ;
                Vec3 terrainNormal;
                Mission.Current.Scene.GetTerrainHeightAndNormal(missilePos.AsVec2, out terrainZ, out terrainNormal);

                float heightDiff = missilePos.z - terrainZ;

                Vec3 spawnPos = missilePos + terrainNormal * 1.1f;

                    item = Game.Current.ObjectManager.GetObject<ItemObject>("nwf_artillery_shell_canister_shot");
                int pelletCount = 40;

                for (int i = 0; i < pelletCount; i++)
                {
                    float angle = MBRandom.RandomFloat * MathF.PI * 2f;

                    float radius = MBRandom.RandomFloat;

                    Vec3 tangent = Vec3.CrossProduct(terrainNormal, Vec3.Up);
                    if (tangent.LengthSquared < 0.001f)
                        tangent = Vec3.Side;

                    tangent = tangent.NormalizedCopy();
                    Vec3 bitangent = Vec3.CrossProduct(terrainNormal, tangent).NormalizedCopy();

                    Vec3 dir =
                        tangent * MathF.Cos(angle) * radius +
                        bitangent * MathF.Sin(angle) * radius +
                        terrainNormal * MBMath.Lerp(0.3f, 1.0f, MBRandom.RandomFloat);

                    dir = dir.NormalizedCopy();

                    // --- SPEED CONTROL ---
                    float speedFactor = MBMath.Lerp(1.0f, 5.0f, MBRandom.RandomFloat);
                    float pelletSpeed = speedFactor;

                    // --- ORIENTATION ---
                    Mat3 orientation = Mat3.Identity;
                    orientation.f = dir;
                    orientation.Orthonormalize();

                    Mission.Current.AddCustomMissile(
                        attackerAgent,
                        new MissionWeapon(item, null, attackerAgent.Origin?.Banner, 1),
                        spawnPos,
                        dir,
                        orientation,
                        pelletSpeed,
                        pelletSpeed,
                        addRigidBody: false,
                        missile.MissionObjectToIgnore
                    );
                }
            }
        }
        public static Vec3 ReflectVector(Vec3 incoming, Vec3 normal)
        {
            normal = normal.NormalizedCopy();
            return incoming - 2f * Vec3.DotProduct(incoming, normal) * normal;
        }
        

        public static Vec3 GetRandomDirection(float deviation, bool fixZ = true)
        {
            float x = MBRandom.RandomFloatRanged(-deviation, deviation);
            var y = MBRandom.RandomFloatRanged(-deviation, deviation);
            var z = fixZ ? 1 : MBRandom.RandomFloatRanged(-deviation, deviation);
            return new Vec3(x, y, z);
        }
        public static Mat3 GetRandomOrientation(Mat3 orientation, float deviation)
        {
            float rand1 = MBRandom.RandomFloatRanged(-deviation, deviation);
            orientation.f.RotateAboutX(rand1);
            float rand2 = MBRandom.RandomFloatRanged(-deviation, deviation);
            orientation.f.RotateAboutY(rand2);
            float rand3 = MBRandom.RandomFloatRanged(-deviation, deviation);
            orientation.f.RotateAboutZ(rand3);
            return orientation;
        }
        public static void SpawnCraterAt(Vec3 position, Mat3 orientation)
        {
            MatrixFrame frame = new MatrixFrame(orientation, position);
            MissionObject prefab = Mission.Current?.CreateMissionObjectFromPrefab("cannonball_crater", frame, entity => {});
        }
    }

}