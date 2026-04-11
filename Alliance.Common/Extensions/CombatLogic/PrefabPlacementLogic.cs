using Alliance.Common.Extensions.UsableEntity.Behaviors;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.CombatLogic
{
    public class PrefabPlacementMissionLogic : MissionLogic
    {

        public PrefabPlacementMissionLogic()
        {
        }
        public static List<MissionObject> SpawnedCannons = new List<MissionObject>();

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

        }
        public void RemoveSpawnedCannons()
        {
            if (Mission.Current?.Scene == null)
                return;
            var allEntitiesWithTag = SpawnedCannons.ToList();


            if (allEntitiesWithTag == null)
                return;

            bool isServer = GameNetwork.IsServer;

            foreach (MissionObject entity in allEntitiesWithTag)
            {
                if (entity == null)
                    continue;

                if (entity.GameEntity.HasTag("placedbyscript"))
                {
                    if (isServer)
                    {
                        GameNetwork.BeginBroadcastModuleEvent();
                        GameNetwork.WriteMessage(new RemoveMissionObject(entity.Id));
                        GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                    }
                    if (entity.GameEntity != null)
                    {
                        entity.GameEntity.Remove(0);
                    }
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
            base.OnClearScene();
            RemoveSpawnedCannons();
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

            Vec3 forward = attackerAgent.LookDirection;
            Vec3 spawnPosGuess = attackerAgent.Position + forward * 1.25f;

            float terrainZ;
            Vec3 terrainNormal;
            Mission.Current.Scene.GetTerrainHeightAndNormal(spawnPosGuess.AsVec2, out terrainZ, out terrainNormal);

            float heightDifference = attackerAgent.Position.Z - terrainZ;

            float heightThreshold = 0.75f;

            Vec3 groundPosition;

            if (Math.Abs(heightDifference) > heightThreshold)
            {
                groundPosition = new Vec3(spawnPosGuess.x, spawnPosGuess.y, attackerAgent.Position.Z);

                forward.z = 0;
                forward = forward.NormalizedCopy();

                Vec3 side = Vec3.CrossProduct(forward, Vec3.Up).NormalizedCopy();

                Mat3 orientation = new Mat3
                {
                    f = forward,
                    s = side,
                    u = Vec3.Up
                };
                orientation.Orthonormalize();

                SpawnPrefabAt(groundPosition, orientation, item.StringId);
            }
            else
            {
                groundPosition = new Vec3(spawnPosGuess.x, spawnPosGuess.y, terrainZ);

                Vec3 projectedForward = forward - Vec3.DotProduct(forward, terrainNormal) * terrainNormal;
                projectedForward = projectedForward.NormalizedCopy();

                Vec3 side = Vec3.CrossProduct(projectedForward, terrainNormal).NormalizedCopy();

                Mat3 orientation = new Mat3
                {
                    f = projectedForward,
                    s = side,
                    u = terrainNormal
                };
                orientation.Orthonormalize();

                SpawnPrefabAt(groundPosition, orientation, item.StringId);
            }
            
        }
        public static void SpawnPrefabAt(Vec3 position, Mat3 orientation, string prefabitem)
        {
            if (!GameNetwork.IsServer)
                return;

            if (Mission.Current == null)
                return;

            MatrixFrame frame = new MatrixFrame(orientation, position);
            string Prefab = prefabitem;
            if (prefabitem != null && prefabitem.Equals("nwf_prefabplacer_12pdbox"))
            {
                MissionObject prefab = Mission.Current?.CreateMissionObjectFromPrefab("nwf_new_ammobox_12pd", frame, entity => {});

                Vec3 child1Position = position + orientation.TransformToParent(new Vec3(0.25f, 0f, 0f));
                MissionObject child_prefab = Mission.Current?.CreateMissionObjectFromPrefab(
                    "nwf_new_ammobox_12pd_ammo12pd", new MatrixFrame(orientation, child1Position), entity => {});

                Vec3 child2Position = position + orientation.TransformToParent(new Vec3(-0.25f, 0f, 0f));
                MissionObject child2_prefab = Mission.Current?.CreateMissionObjectFromPrefab(
                    "nwf_new_ammobox_ammocanister", new MatrixFrame(orientation, child2Position), entity => {});
                prefab.GameEntity.AddChild(child_prefab.GameEntity, true);
                prefab.GameEntity.AddChild(child2_prefab.GameEntity, true);
                prefab.GameEntity.AddTag("placedbyscript");
                child_prefab.GameEntity.AddTag("placedbyscript");
                child2_prefab.GameEntity.AddTag("placedbyscript");

            }
            if (prefabitem != null && prefabitem.Equals("nwf_prefabplacer_howitzerbox"))
            {
                MissionObject prefab = Mission.Current?.CreateMissionObjectFromPrefab("nwf_new_ammobox_howitzer", frame, entity => {});

                Vec3 child1Position = position + orientation.TransformToParent(new Vec3(0.25f, 0f, 0f));
                MissionObject child_prefab = Mission.Current?.CreateMissionObjectFromPrefab(
                    "nwf_new_ammobox_howitzer_ammohowitzer", new MatrixFrame(orientation, child1Position), entity => {});

                Vec3 child2Position = position + orientation.TransformToParent(new Vec3(-0.25f, 0f, 0f));
                MissionObject child2_prefab = Mission.Current?.CreateMissionObjectFromPrefab(
                    "nwf_new_ammobox_ammocanister", new MatrixFrame(orientation, child2Position), entity => {});
                prefab.GameEntity.AddChild(child_prefab.GameEntity, true);
                prefab.GameEntity.AddChild(child2_prefab.GameEntity, true);
                prefab.GameEntity.AddTag("placedbyscript");
                child_prefab.GameEntity.AddTag("placedbyscript");
                child2_prefab.GameEntity.AddTag("placedbyscript");
            }
            if (prefabitem != null && prefabitem.Equals("nwf_prefabplacer_12pdcannonbox"))
            {
                MissionObject cannonprefab = Mission.Current?.CreateMissionObjectFromPrefab("nwf_cannon_12pd_moveable", frame, entity => {});
                cannonprefab.GameEntity.AddTag("placedbyscript");
                SpawnedCannons.Add(cannonprefab);
            }
            if (prefabitem != null && prefabitem.Equals("nwf_prefabplacer_howitzercannonbox"))
            {
                MissionObject howitzerprefab = Mission.Current?.CreateMissionObjectFromPrefab("nwf_cannon_howitzer_moveable", frame, entity => {});
                howitzerprefab.GameEntity.AddTag("placedbyscript");
                SpawnedCannons.Add(howitzerprefab);
            }
        }
    }

}