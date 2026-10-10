using Alliance.Common.Extensions.UsableEntity.Behaviors;
using NetworkMessages.FromServer;
using Alliance.Common.Extensions.SapperLogics;
using Alliance.Common.Extensions.SapperLogics.NetworkMessages.FromServer;
using Alliance.Common.Core.Configuration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Alliance.Common.Extensions.PE;

namespace Alliance.Common.Extensions.CombatLogic
{
    public class PrefabPlacementMissionLogic : MissionNetwork
    {
        private const int MaximumSapperBuildingPoints = 100000;

        private sealed class PendingSapperBuildingRequest
        {
            public Agent BuilderAgent { get; set; }
            public string PrefabId { get; set; }
        }

        private readonly Queue<PendingSapperBuildingRequest> _pendingSapperBuildingRequests =
            new Queue<PendingSapperBuildingRequest>();
        private readonly HashSet<Agent> _agentsWithPendingSapperBuildings = new HashSet<Agent>();
        private readonly object _pendingSapperBuildingRequestsLock = new object();
        private readonly Dictionary<BattleSideEnum, int> _sapperBuildPoints = new Dictionary<BattleSideEnum, int>();
        private readonly HashSet<BattleSideEnum> _pendingSapperBuildPointUpdates = new HashSet<BattleSideEnum>();
        private readonly object _sapperBuildPointsLock = new object();
        private MultiplayerRoundController _subscribedRoundController;
        private bool _sapperBuildPointsInitialized;

        public PrefabPlacementMissionLogic()
        {
        }
        public static List<MissionObject> SpawnedCannons = new List<MissionObject>();

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            if (GameNetwork.IsServer)
            {
                ResetSapperBuildingPointsToAssignedValue();
            }

            SubscribeToRoundController();
        }

        public override void OnRemoveBehavior()
        {
            if (_subscribedRoundController != null)
            {
                _subscribedRoundController.OnRoundStarted -= ResetSapperBuildingPointsToAssignedValue;
                _subscribedRoundController = null;
            }

            base.OnRemoveBehavior();
        }

        protected override void HandleLateNewClientAfterSynchronized(NetworkCommunicator networkPeer)
        {
            base.HandleLateNewClientAfterSynchronized(networkPeer);
            if (!GameNetwork.IsServer || networkPeer == null)
            {
                return;
            }

            EnsureSapperBuildPointsInitialized();
            lock (_sapperBuildPointsLock)
            {
                foreach (KeyValuePair<BattleSideEnum, int> balance in _sapperBuildPoints)
                {
                    GameNetwork.BeginModuleEventAsServer(networkPeer);
                    GameNetwork.WriteMessage(new SapperBuildingPointsUpdate(balance.Key, balance.Value));
                    GameNetwork.EndModuleEventAsServer();
                }
            }
        }

        private void SubscribeToRoundController()
        {
            MultiplayerRoundController roundController = Mission?.GetMissionBehavior<MultiplayerRoundController>();
            if (roundController == null || roundController == _subscribedRoundController)
            {
                return;
            }

            if (_subscribedRoundController != null)
            {
                _subscribedRoundController.OnRoundStarted -= ResetSapperBuildingPointsToAssignedValue;
            }

            _subscribedRoundController = roundController;
            _subscribedRoundController.OnRoundStarted += ResetSapperBuildingPointsToAssignedValue;
        }

        private void EnsureSapperBuildPointsInitialized()
        {
            if (_sapperBuildPointsInitialized)
            {
                return;
            }

            ResetSapperBuildingPointsToAssignedValue();
        }

        /// <summary>
        /// Resets round balances from the configured assigned value. The active balances are
        /// kept separately so spending during a round never mutates the persistent setting.
        /// </summary>
        public void ResetSapperBuildingPointsToAssignedValue()
        {
            if (!GameNetwork.IsServer)
            {
                return;
            }

            int assignedPoints = GetAssignedSapperBuildingPoints();
            lock (_sapperBuildPointsLock)
            {
                _sapperBuildPoints[BattleSideEnum.Attacker] = assignedPoints;
                _sapperBuildPoints[BattleSideEnum.Defender] = assignedPoints;
                _pendingSapperBuildPointUpdates.Add(BattleSideEnum.Attacker);
                _pendingSapperBuildPointUpdates.Add(BattleSideEnum.Defender);
                _sapperBuildPointsInitialized = true;
            }
        }

        /// <summary>
        /// Returns the current team pool on either server or client. Clients fall back to the
        /// assigned value until the server's balance sync has arrived.
        /// </summary>
        public int GetSapperBuildingPoints(BattleSideEnum side)
        {
            lock (_sapperBuildPointsLock)
            {
                if (_sapperBuildPoints.TryGetValue(side, out int points))
                {
                    return points;
                }
            }

            return GetAssignedSapperBuildingPoints();
        }

        /// <summary>
        /// Applies a server balance update on a client.
        /// </summary>
        public void SetClientSapperBuildingPoints(BattleSideEnum side, int points)
        {
            if (!GameNetwork.IsClient || (side != BattleSideEnum.Attacker && side != BattleSideEnum.Defender) || points < 0)
            {
                return;
            }

            lock (_sapperBuildPointsLock)
            {
                _sapperBuildPoints[side] = points;
                _sapperBuildPointsInitialized = true;
            }
        }

        private bool TrySpendSapperBuildingPoints(BattleSideEnum side, int cost)
        {
            if (!GameNetwork.IsServer || cost <= 0 ||
                (side != BattleSideEnum.Attacker && side != BattleSideEnum.Defender))
            {
                return false;
            }

            EnsureSapperBuildPointsInitialized();
            int remainingPoints;
            lock (_sapperBuildPointsLock)
            {
                if (!_sapperBuildPoints.TryGetValue(side, out int points) || points < cost)
                {
                    return false;
                }

                remainingPoints = points - cost;
                _sapperBuildPoints[side] = remainingPoints;
                _pendingSapperBuildPointUpdates.Add(side);
            }
            return true;
        }

        private void RefundSapperBuildingPoints(BattleSideEnum side, int cost)
        {
            int restoredPoints;
            lock (_sapperBuildPointsLock)
            {
                restoredPoints = (_sapperBuildPoints.TryGetValue(side, out int currentPoints)
                    ? currentPoints
                    : GetAssignedSapperBuildingPoints()) + cost;
                _sapperBuildPoints[side] = restoredPoints;
                _pendingSapperBuildPointUpdates.Add(side);
            }
        }

        private void FlushPendingSapperBuildPointUpdates()
        {
            if (!GameNetwork.IsServer)
            {
                return;
            }

            List<KeyValuePair<BattleSideEnum, int>> updates = new List<KeyValuePair<BattleSideEnum, int>>();
            lock (_sapperBuildPointsLock)
            {
                foreach (BattleSideEnum side in _pendingSapperBuildPointUpdates)
                {
                    if (_sapperBuildPoints.TryGetValue(side, out int points))
                    {
                        updates.Add(new KeyValuePair<BattleSideEnum, int>(side, points));
                    }
                }
                _pendingSapperBuildPointUpdates.Clear();
            }

            foreach (KeyValuePair<BattleSideEnum, int> update in updates)
            {
                BroadcastSapperBuildingPoints(update.Key, update.Value);
            }
        }

        private static void BroadcastSapperBuildingPoints(BattleSideEnum side, int points)
        {
            if (!GameNetwork.IsServer)
            {
                return;
            }

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SapperBuildingPointsUpdate(side, points));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }

        private static int GetAssignedSapperBuildingPoints()
        {
            return Math.Max(0, Math.Min(MaximumSapperBuildingPoints, Config.Instance?.SapperBuildingPoints ?? 100));
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            SubscribeToRoundController();

            if (!GameNetwork.IsServer)
            {
                return;
            }

            PendingSapperBuildingRequest request = null;
            lock (_pendingSapperBuildingRequestsLock)
            {
                if (_pendingSapperBuildingRequests.Count > 0)
                {
                    request = _pendingSapperBuildingRequests.Dequeue();
                    _agentsWithPendingSapperBuildings.Remove(request.BuilderAgent);
                }
            }

            if (request != null)
            {
                TrySpawnSapperBuilding(request.BuilderAgent, request.PrefabId);
            }

            FlushPendingSapperBuildPointUpdates();
        }
        public static void RemoveSpawnedCannons()
        {
            if (Mission.Current?.Scene == null)
                return;
            var allEntitiesWithTag = SpawnedCannons.ToList();


            if (allEntitiesWithTag == null)
                return;

            foreach (MissionObject entity in allEntitiesWithTag)
            {
                if (entity == null || entity.GameEntity == null || !entity.GameEntity.HasTag("cannonbyscript"))
                    continue;
                    if (!GameNetwork.IsClientOrReplay)
                    {
                        GameNetwork.BeginBroadcastModuleEvent();
                        GameNetwork.WriteMessage(new RemoveMissionObject(entity.Id));
                        GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None); 
                }
                    if (entity != null && entity.GameEntity != null)
                    {
                        entity.GameEntity.Remove(0);
                    }
            }
            foreach (Agent agent in Mission.Current.Agents)
            {
                if (agent == null || !agent.IsActive() || !agent.IsHuman)
                    continue;

                if (agent.IsUsingGameObject)
                {
                    WeakGameEntity usedEntity = agent.CurrentlyUsedGameObject.GameEntity;

                    if (usedEntity == null || usedEntity.Scene == null)
                    {
                        agent.StopUsingGameObject();
                    }
                }
            }
        }
        public override void OnClearScene()
        {
            lock (_pendingSapperBuildingRequestsLock)
            {
                _pendingSapperBuildingRequests.Clear();
                _agentsWithPendingSapperBuildings.Clear();
            }

            base.OnClearScene();
        }   
        public override void OnMissileCollisionReaction(
    Mission.MissileCollisionReaction collisionReaction,
    Agent attackerAgent,
    Agent attachedAgent,
    sbyte attachedBoneIndex)
        {
            base.OnMissileCollisionReaction(collisionReaction, attackerAgent, attachedAgent, attachedBoneIndex);

            var missile = Mission.MissilesList.FirstOrDefault(m => m.ShooterAgent == attackerAgent);
            if (missile == null)
                return;

            var item = missile.Weapon.Item;
            if (item == null || !item.StringId.Contains("nwf_prefabplacer"))
                return;

            if (collisionReaction != Mission.MissileCollisionReaction.BecomeInvisible)
                return;

            MatrixFrame placementFrame;
            if (TryGetAgentPlacementFrame(attackerAgent, out placementFrame))
            {
                SpawnPrefabAt(placementFrame.origin, placementFrame.rotation, item.StringId);
            }
            
        }

        /// <summary>
        /// Checks the client-facing sapper menu requirements. The server calls this again
        /// before accepting a placement request.
        /// </summary>
        public static bool CanAgentUseSapperBuildingMenu(Agent agent)
        {
            if (agent == null || !agent.IsActive() || !agent.IsHuman || agent.Character == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(agent.Character.StringId) ||
                agent.Character.StringId.IndexOf("sapper", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            return IsSapperHammer(agent.WieldedWeapon) || IsSapperHammer(agent.WieldedOffhandWeapon);
        }

        /// <summary>
        /// Server-authoritative entry point for UI-based sapper placement.
        /// The client only sends the prefab ID; position and orientation come from the server's agent state.
        /// </summary>
        public static bool TrySpawnSapperBuilding(Agent builderAgent, string prefabId)
        {
            if (!GameNetwork.IsServer || Mission.Current == null ||
                !CanAgentUseSapperBuildingMenu(builderAgent) ||
                !SapperBuildingCatalog.ContainsPrefab(prefabId))
            {
                return false;
            }

            MatrixFrame placementFrame;
            if (!TryGetSapperBuildingPlacementFrame(builderAgent, out placementFrame))
            {
                return false;
            }

            PrefabPlacementMissionLogic missionLogic = Mission.Current.GetMissionBehavior<PrefabPlacementMissionLogic>();
            BattleSideEnum teamSide = builderAgent.Team?.Side ?? BattleSideEnum.None;
            int buildCost = SapperBuildingCatalog.GetBuildCost(prefabId);
            if (missionLogic == null || !missionLogic.TrySpendSapperBuildingPoints(teamSide, buildCost))
            {
                return false;
            }

            MissionObject building;
            try
            {
                building = Mission.Current.CreateMissionObjectFromPrefab(
                    prefabId,
                    placementFrame,
                    false,
                    0,
                    entity => { });
            }
            catch
            {
                missionLogic.RefundSapperBuildingPoints(teamSide, buildCost);
                throw;
            }

            if (building == null || building.GameEntity == null)
            {
                missionLogic.RefundSapperBuildingPoints(teamSide, buildCost);
                return false;
            }

            building.GameEntity.AddTag("placedbyscript");
            return true;
        }

        /// <summary>
        /// Queues a validated request so native prefab creation runs on the mission tick,
        /// after the incoming module message has finished processing.
        /// </summary>
        public static bool QueueSapperBuildingRequest(Agent builderAgent, string prefabId)
        {
            if (!GameNetwork.IsServer || Mission.Current == null ||
                !CanAgentUseSapperBuildingMenu(builderAgent) ||
                !SapperBuildingCatalog.ContainsPrefab(prefabId))
            {
                return false;
            }

            PrefabPlacementMissionLogic missionLogic =
                Mission.Current.GetMissionBehavior<PrefabPlacementMissionLogic>();
            if (missionLogic == null)
            {
                return false;
            }

            lock (missionLogic._pendingSapperBuildingRequestsLock)
            {
                if (!missionLogic._agentsWithPendingSapperBuildings.Add(builderAgent))
                {
                    return false;
                }

                missionLogic._pendingSapperBuildingRequests.Enqueue(new PendingSapperBuildingRequest
                {
                    BuilderAgent = builderAgent,
                    PrefabId = prefabId
                });
            }

            return true;
        }

        private static bool IsSapperHammer(in MissionWeapon weapon)
        {
            return !weapon.IsEmpty && weapon.Item != null && weapon.Item.StringId == "nwf_sapper_hammer";
        }

        /// <summary>
        /// Uses the same forward offset and terrain-normal alignment as artillery box placement.
        /// </summary>
        private static bool TryGetAgentPlacementFrame(Agent agent, out MatrixFrame frame)
        {
            frame = MatrixFrame.Identity;
            if (agent == null || Mission.Current?.Scene == null)
            {
                return false;
            }

            Vec3 forward = agent.LookDirection;
            Vec3 spawnPosition = agent.Position + forward * 1.25f;

            float terrainZ;
            Vec3 terrainNormal;
            Mission.Current.Scene.GetTerrainHeightAndNormal(spawnPosition.AsVec2, out terrainZ, out terrainNormal);

            if (terrainNormal.LengthSquared < 0.0001f)
            {
                terrainNormal = Vec3.Up;
            }
            else
            {
                terrainNormal = terrainNormal.NormalizedCopy();
            }

            Vec3 groundPosition;
            Mat3 orientation;
            float heightDifference = agent.Position.Z - terrainZ;

            if (Math.Abs(heightDifference) > 0.75f)
            {
                groundPosition = new Vec3(spawnPosition.x, spawnPosition.y, agent.Position.Z);

                forward.z = 0;
                if (forward.LengthSquared < 0.0001f)
                {
                    forward = new Vec3(1f, 0f, 0f);
                }
                forward = forward.NormalizedCopy();

                Vec3 side = Vec3.CrossProduct(forward, Vec3.Up).NormalizedCopy();
                orientation = new Mat3
                {
                    f = forward,
                    s = side,
                    u = Vec3.Up
                };
            }
            else
            {
                groundPosition = new Vec3(spawnPosition.x, spawnPosition.y, terrainZ);

                Vec3 projectedForward = forward - Vec3.DotProduct(forward, terrainNormal) * terrainNormal;
                if (projectedForward.LengthSquared < 0.0001f)
                {
                    projectedForward = forward;
                    projectedForward.z = 0;
                }
                if (projectedForward.LengthSquared < 0.0001f)
                {
                    projectedForward = new Vec3(1f, 0f, 0f);
                }
                projectedForward = projectedForward.NormalizedCopy();

                Vec3 side = Vec3.CrossProduct(projectedForward, terrainNormal).NormalizedCopy();
                orientation = new Mat3
                {
                    f = projectedForward,
                    s = side,
                    u = terrainNormal
                };
            }

            orientation.Orthonormalize();
            frame = new MatrixFrame(orientation, groundPosition);
            return true;
        }

        /// <summary>
        /// Places sapper buildings on the first collision below the forward placement point.
        /// This allows an existing building to support the next one while preventing floating
        /// placements and surfaces more than five metres above the builder.
        /// </summary>
        private static bool TryGetSapperBuildingPlacementFrame(Agent agent, out MatrixFrame frame)
        {
            frame = MatrixFrame.Identity;
            Scene scene = Mission.Current?.Scene;
            if (agent == null || scene == null)
            {
                return false;
            }

            Vec3 forward = agent.LookDirection;
            forward.z = 0f;
            if (forward.LengthSquared < 0.0001f)
            {
                return false;
            }
            forward = forward.NormalizedCopy();

            Vec3 placementPoint = agent.Position + forward * 1.25f;
            float terrainZ;
            Vec3 terrainNormal;
            scene.GetTerrainHeightAndNormal(placementPoint.AsVec2, out terrainZ, out terrainNormal);

            // Start high enough to detect an over-height collision instead of accidentally
            // placing beneath it, and end just below terrain so unsupported points fail.
            Vec3 rayStart = new Vec3(placementPoint.x, placementPoint.y,
                Math.Max(agent.Position.Z, terrainZ) + 50f);
            Vec3 rayEnd = new Vec3(placementPoint.x, placementPoint.y, terrainZ - 1f);
            Vec3 hitPoint;
            WeakGameEntity hitEntity;
            bool hasSupport;
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                hasSupport = scene.RayCastForClosestEntityOrTerrain(
                    rayStart,
                    rayEnd,
                    out _,
                    out hitPoint,
                    out hitEntity);
            }

            if (!hasSupport || hitPoint.z > agent.Position.Z + 5f)
            {
                return false;
            }

            if (hitEntity != null)
            {
                // Entity raycasts return the collision point but not a surface normal. Built
                // sapper props have flat support surfaces, so keep them level when stacking.
                terrainNormal = Vec3.Up;
            }
            else if (terrainNormal.LengthSquared < 0.0001f)
            {
                terrainNormal = Vec3.Up;
            }
            else
            {
                terrainNormal = terrainNormal.NormalizedCopy();
            }

            Vec3 projectedForward = forward - Vec3.DotProduct(forward, terrainNormal) * terrainNormal;
            if (projectedForward.LengthSquared < 0.0001f)
            {
                projectedForward = forward;
            }
            projectedForward = projectedForward.NormalizedCopy();

            Vec3 side = Vec3.CrossProduct(projectedForward, terrainNormal).NormalizedCopy();
            Mat3 orientation = new Mat3
            {
                f = projectedForward,
                s = side,
                u = terrainNormal
            };
            orientation.Orthonormalize();
            frame = new MatrixFrame(orientation, hitPoint);
            return true;
        }

        public static void SpawnPrefabAt(Vec3 position, Mat3 orientation, string prefabitem)
        {
            if (!GameNetwork.IsServer)
                return;

            if (Mission.Current == null)
                return;

            MatrixFrame frame = new MatrixFrame(orientation, position);
            if (prefabitem != null && prefabitem.Equals("nwf_prefabplacer_12pdbox"))
            {
                MissionObject prefab = Mission.Current?.CreateMissionObjectFromPrefab("nwf_new_ammobox_12pd", frame,
                false, 0, entity => {});

                Vec3 child1Position = position + orientation.TransformToParent(new Vec3(0.25f, 0f, 0f));
                MissionObject child_prefab = Mission.Current?.CreateMissionObjectFromPrefab(
                    "nwf_new_ammobox_12pd_ammo12pd", new MatrixFrame(orientation, child1Position),
                false, 0, entity => {});

                Vec3 child2Position = position + orientation.TransformToParent(new Vec3(-0.25f, 0f, 0f));
                MissionObject child2_prefab = Mission.Current?.CreateMissionObjectFromPrefab(
                    "nwf_new_ammobox_ammocanister", new MatrixFrame(orientation, child2Position),
                false, 0, entity => {});
                prefab.GameEntity.AddChild(child_prefab.GameEntity, true);
                prefab.GameEntity.AddChild(child2_prefab.GameEntity, true);
                prefab.GameEntity.AddTag("placedbyscript");
                child_prefab.GameEntity.AddTag("placedbyscript");
                child2_prefab.GameEntity.AddTag("placedbyscript");

            }
            if (prefabitem != null && prefabitem.Equals("nwf_prefabplacer_howitzerbox"))
            {
                MissionObject prefab = Mission.Current?.CreateMissionObjectFromPrefab("nwf_new_ammobox_howitzer", frame,
                false, 0, entity => {});

                Vec3 child1Position = position + orientation.TransformToParent(new Vec3(0.25f, 0f, 0f));
                MissionObject child_prefab = Mission.Current?.CreateMissionObjectFromPrefab(
                    "nwf_new_ammobox_howitzer_ammohowitzer", new MatrixFrame(orientation, child1Position),
                false, 0, entity => {});

                Vec3 child2Position = position + orientation.TransformToParent(new Vec3(-0.25f, 0f, 0f));
                MissionObject child2_prefab = Mission.Current?.CreateMissionObjectFromPrefab(
                    "nwf_new_ammobox_ammocanister", new MatrixFrame(orientation, child2Position),
                false, 0, entity => {});
                prefab.GameEntity.AddChild(child_prefab.GameEntity, true);
                prefab.GameEntity.AddChild(child2_prefab.GameEntity, true);
                prefab.GameEntity.AddTag("placedbyscript");
                child_prefab.GameEntity.AddTag("placedbyscript");
                child2_prefab.GameEntity.AddTag("placedbyscript");
            }
            if (prefabitem != null && prefabitem.Equals("nwf_prefabplacer_12pdcannonbox"))
            {
                MissionObject cannonprefab = Mission.Current?.CreateMissionObjectFromPrefab("limber_cannon", frame,
                false, 0, entity => {});
                cannonprefab.GameEntity.AddTag("cannonbyscript");
                SpawnedCannons.Add(cannonprefab);
            }
            if (prefabitem != null && prefabitem.Equals("nwf_prefabplacer_howitzercannonbox"))
            {
                MissionObject howitzerprefab = Mission.Current?.CreateMissionObjectFromPrefab("nwf_cannon_howitzer_moveable", frame,
                false, 0, entity => {});
                howitzerprefab.GameEntity.AddTag("cannonbyscript");
                SpawnedCannons.Add(howitzerprefab);
            }
        }
    }

}
