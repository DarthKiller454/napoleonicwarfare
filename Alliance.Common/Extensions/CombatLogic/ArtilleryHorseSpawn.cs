using Alliance.Common.Extensions.PE;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.CombatLogic
{
    public sealed class AutoLimberCannonSpawnerBehavior : MissionLogic
    {
        private const string TargetHorseItemId = "artillery_horse";

        private const string CannonBoxItemId = "nwf_prefabplacer_12pdcannonbox";
        private const string HowitzerBoxItemId = "nwf_prefabplacer_howitzercannonbox";

        private const string CannonPrefabId = "limber_cannon";
        private const string HowitzerPrefabId = "limber_howitzer";

        private static readonly EquipmentIndex[] WeaponSlotsToCheck =
        {
            EquipmentIndex.Weapon0,
            EquipmentIndex.Weapon1,
            EquipmentIndex.Weapon2,
            EquipmentIndex.Weapon3
        };

        private struct LimberSpawnRequest
        {
            public string SourceItemId;
            public string PrefabId;
            public EquipmentIndex SourceSlot;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);

            if (GameNetwork.IsClientOrReplay)
                return;

            if (agent == null || !agent.IsHuman)
                return;

            if (!HasTargetHorseItem(agent))
                return;

            LimberSpawnRequest spawnRequest;

            if (!TryGetLimberSpawnRequest(agent, out spawnRequest))
                return;

            Agent horseAgent = agent.MountAgent;

            if (horseAgent == null || !horseAgent.IsActive())
            {
                Debug.Print("[AUTO LIMBER] Rider has target horse item and cannon box, but MountAgent is not ready yet.");
                return;
            }

            SpawnAndAttachCannon(agent, horseAgent, spawnRequest);
        }

        private void SpawnAndAttachCannon(
            Agent riderAgent,
            Agent horseAgent,
            LimberSpawnRequest spawnRequest)
        {
            if (riderAgent == null || !riderAgent.IsActive())
                return;

            if (horseAgent == null || !horseAgent.IsActive())
                return;

            MatrixFrame frame = MatrixFrame.Identity;
            frame.origin = horseAgent.Position;

            Vec3 terrainNormal;
            float terrainZ;
            Mission.Scene.GetTerrainHeightAndNormal(frame.origin.AsVec2, out terrainZ, out terrainNormal);

            frame.origin.z = terrainZ;

            MissionObject cannonMissionObject = Mission.CreateMissionObjectFromPrefab(
                spawnRequest.PrefabId,
                frame,
                entity => { });

            if (cannonMissionObject == null)
            {
                Debug.Print("[AUTO LIMBER] Failed to spawn prefab: " + spawnRequest.PrefabId);
                return;
            }

            WeakGameEntity rootEntity = cannonMissionObject.GameEntity;

            if (!rootEntity.IsValid)
            {
                Debug.Print("[AUTO LIMBER] Spawned prefab has invalid root entity: " + spawnRequest.PrefabId);
                return;
            }

            PE_AttachToAgent attachScript = FindAttachScript(rootEntity);

            if (attachScript == null)
            {
                Debug.Print("[AUTO LIMBER] No PE_AttachToAgent found on spawned prefab: " + spawnRequest.PrefabId);
                return;
            }

            riderAgent.UseGameObject(attachScript, -1);
            AgentFlag flags = horseAgent.GetAgentFlags();

            flags &= ~(AgentFlag.CanJump |
                       AgentFlag.RunsAwayWhenHit |
                       AgentFlag.CanWander);

            horseAgent.SetAgentFlags(flags);

            RemoveLimberSpawnerItemFromAgent(riderAgent, spawnRequest);

            Debug.Print(
                "[AUTO LIMBER] Spawned and attached prefab: " +
                spawnRequest.PrefabId +
                " from item: " +
                spawnRequest.SourceItemId);
        }

        private static bool TryGetLimberSpawnRequest(
            Agent riderAgent,
            out LimberSpawnRequest spawnRequest)
        {
            spawnRequest = default(LimberSpawnRequest);

            if (riderAgent == null || !riderAgent.IsHuman)
                return false;

            Equipment spawnEquipment = riderAgent.SpawnEquipment;

            if (spawnEquipment == null)
                return false;

            foreach (EquipmentIndex equipmentIndex in WeaponSlotsToCheck)
            {
                EquipmentElement equipmentElement = spawnEquipment[equipmentIndex];
                ItemObject item = equipmentElement.Item;

                if (item == null)
                    continue;

                if (item.StringId == CannonBoxItemId)
                {
                    spawnRequest = new LimberSpawnRequest
                    {
                        SourceItemId = CannonBoxItemId,
                        PrefabId = CannonPrefabId,
                        SourceSlot = equipmentIndex
                    };

                    return true;
                }

                if (item.StringId == HowitzerBoxItemId)
                {
                    spawnRequest = new LimberSpawnRequest
                    {
                        SourceItemId = HowitzerBoxItemId,
                        PrefabId = HowitzerPrefabId,
                        SourceSlot = equipmentIndex
                    };

                    return true;
                }
            }

            return false;
        }

        private static void RemoveLimberSpawnerItemFromAgent(
            Agent riderAgent,
            LimberSpawnRequest spawnRequest)
        {
            if (riderAgent == null || !riderAgent.IsActive())
                return;

            if (TryRemoveSpawnerItemFromSlot(riderAgent, spawnRequest.SourceSlot, spawnRequest.SourceItemId))
                return;

            foreach (EquipmentIndex equipmentIndex in WeaponSlotsToCheck)
            {
                if (equipmentIndex == spawnRequest.SourceSlot)
                    continue;

                if (TryRemoveSpawnerItemFromSlot(riderAgent, equipmentIndex, spawnRequest.SourceItemId))
                    return;
            }

            Debug.Print("[AUTO LIMBER] Could not remove cannon box item from agent: " + spawnRequest.SourceItemId);
        }

        private static bool TryRemoveSpawnerItemFromSlot(
            Agent riderAgent,
            EquipmentIndex equipmentIndex,
            string itemId)
        {
            if (riderAgent == null)
                return false;

            MissionWeapon missionWeapon = riderAgent.Equipment[equipmentIndex];
            ItemObject item = missionWeapon.Item;

            if (item == null)
                return false;

            if (item.StringId != itemId)
                return false;

            riderAgent.RemoveEquippedWeapon(equipmentIndex);

            Debug.Print("[AUTO LIMBER] Removed cannon box item from slot: " + equipmentIndex + " item: " + itemId);

            return true;
        }

        private static bool HasTargetHorseItem(Agent riderAgent)
        {
            if (riderAgent == null)
                return false;

            if (!riderAgent.IsHuman)
                return false;

            Equipment spawnEquipment = riderAgent.SpawnEquipment;

            if (spawnEquipment == null)
                return false;

            EquipmentElement horseElement = spawnEquipment[EquipmentIndex.Horse];

            ItemObject horseItem = horseElement.Item;

            if (horseItem == null)
                return false;

            return horseItem.StringId == TargetHorseItemId;
        }

        private static PE_AttachToAgent FindAttachScript(WeakGameEntity rootEntity)
        {
            if (!rootEntity.IsValid)
                return null;

            PE_AttachToAgent attachScript = rootEntity.GetFirstScriptOfType<PE_AttachToAgent>();

            if (attachScript != null)
                return attachScript;

            List<WeakGameEntity> children = new List<WeakGameEntity>();
            rootEntity.GetChildrenRecursive(ref children);

            foreach (WeakGameEntity child in children)
            {
                if (!child.IsValid)
                    continue;

                attachScript = child.GetFirstScriptOfType<PE_AttachToAgent>();

                if (attachScript != null)
                    return attachScript;
            }

            return null;
        }
    }
}