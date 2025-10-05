using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.PE
{
    public class PE_PrefabSpawner : UsableMissionObject
    {
        public override TickRequirement GetTickRequirement()
        {
            if (!GameNetwork.IsServer)
            {
                return TickRequirement.Tick | TickRequirement.TickParallel;
            }
            return base.GetTickRequirement();
        }

        public List<SpawnableItem> SpawnableItems { get; private set; }

        public List<GameEntity> SpawnedPrefabs { get; private set; }

        public Dictionary<GameEntity, IStray> StrayEntity { get; private set; }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);
            if (GameNetwork.IsServer)
            {
                foreach (GameEntity gameEntity in StrayEntity.Keys.ToList())
                {
                    if (gameEntity == null)
                    {
                        StrayEntity.Remove(gameEntity);
                        SpawnedPrefabs.Remove(gameEntity);
                    }
                    else if (StrayEntity[gameEntity].IsStray())
                    {
                        DespawnSpawnedPrefab(gameEntity);
                    }
                }
            }
        }

        protected void LoadSpawnableItems()
        {
            string xmlPath = ModuleHelper.GetXmlPath("Napoleonic Warfare MP", "PrefabSpawner/" + SpawningPrefabsXml);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlPath);
            foreach (object obj in xmlDocument.SelectNodes("/SpawnItems/SpawnItem"))
            {
                XmlNode xmlNode = (XmlNode)obj;
                string innerText = xmlNode["ItemId"].InnerText;
                string innerText2 = xmlNode["PrefabName"].InnerText;
                int maxSpawnAmount = xmlNode["MaxSpawnAmount"] != null ? int.Parse(xmlNode["MaxSpawnAmount"].InnerText) : 20;
                float despawnArea = xmlNode["DespawnArea"] != null ? float.Parse(xmlNode["DespawnArea"].InnerText) : 5f;
                float adjustPositionX = xmlNode["AdjustPositionX"] != null ? float.Parse(xmlNode["AdjustPositionX"].InnerText) : 0f;
                float adjustPositionY = xmlNode["AdjustPositionY"] != null ? float.Parse(xmlNode["AdjustPositionY"].InnerText) : 0f;
                float adjustPositionZ = xmlNode["AdjustPositionZ"] != null ? float.Parse(xmlNode["AdjustPositionZ"].InnerText) : 0f;
                SpawnableItem spawnableItem = new SpawnableItem(innerText2, innerText, maxSpawnAmount, despawnArea, adjustPositionX, adjustPositionY, adjustPositionZ);
                if (spawnableItem.SpawnerItem != null)
                {
                    SpawnableItems.Add(spawnableItem);
                }
            }
        }

        protected override void OnInit()
        {
            base.OnInit();
            TextObject textObject = new TextObject("Use {PrefabSpawnerName} To Spawn {SpawnerCategoryName}", null);
            textObject.SetTextVariable("PrefabSpawnerName", PrefabSpawnerName);
            textObject.SetTextVariable("SpawnerCategoryName", SpawnerCategoryName);
            ActionMessage = textObject;
            TextObject textObject2 = new TextObject("Press {KEY} To Interact", null);
            textObject2.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            DescriptionMessage = textObject2;
            SpawnableItems = new List<SpawnableItem>();
            LoadSpawnableItems();
            SpawningPoint = GameEntity.GetFirstChildEntityWithTag(SpawnPointTag);
            SpawnedPrefabs = new List<GameEntity>();
            StrayEntity = new Dictionary<GameEntity, IStray>();
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            return "Siege Workshop";
        }

        public override void OnUse(Agent userAgent)
        {
            base.OnUse(userAgent);
            userAgent.StopUsingGameObjectMT(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
            if (GameNetwork.IsServer)
            {
                Debug.Print("[USING LOG] AGENT USE " + GetType().Name, 0, Debug.DebugColor.White, 17592186044416UL);
                EquipmentIndex wieldedItemIndex = userAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                if (wieldedItemIndex == EquipmentIndex.None)
                {
                    DespawnNearest(userAgent);
                    return;
                }
                MissionWeapon missionWeapon = userAgent.Equipment[wieldedItemIndex];
                SpawnableItem spawnableItem = SpawnableItems.FirstOrDefault((s) => s.SpawnerItem.Id == missionWeapon.Item.Id);
                if (spawnableItem.SpawnerItem == null)
                {
                    DespawnNearest(userAgent);
                    return;
                }
                if (SpawnedPrefabs.Count < spawnableItem.MaxSpawnAmount)
                {
                    SpawnSpawnableItem(userAgent, spawnableItem);
                }
            }
        }

        private void SpawnSpawnableItem(Agent userAgent, SpawnableItem spawnableItem)
        {
            EquipmentIndex wieldedItemIndex = userAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            userAgent.RemoveEquippedWeapon(wieldedItemIndex);
            MatrixFrame globalFrame = SpawningPoint.GetGlobalFrame();
            Vec3 o = new Vec3(globalFrame.origin.X + spawnableItem.AdjustPositionX, globalFrame.origin.Y + spawnableItem.AdjustPositionY, globalFrame.origin.Z + spawnableItem.AdjustPositionZ, -1f);
            MatrixFrame frame = new MatrixFrame(globalFrame.rotation, o);
            MissionObject missionObject = Mission.Current.CreateMissionObjectFromPrefab(spawnableItem.PrefabName, frame);
            SpawnedPrefabs.Add(missionObject.GameEntity);
            List<GameEntity> list = new List<GameEntity>();
            ScriptComponentBehavior[] array = (from s in missionObject.GameEntity.GetScriptComponents()
                                               where s is ISpawnable
                                               select s).ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                ((ISpawnable)array[i]).OnSpawnedByPrefab(this);
            }
            missionObject.GameEntity.GetChildrenRecursive(ref list);
            foreach (GameEntity gameEntity in list)
            {
                array = (from s in gameEntity.GetScriptComponents()
                         where s is ISpawnable
                         select s).ToArray();
                for (int j = 0; j < array.Length; j++)
                {
                    ((ISpawnable)array[j]).OnSpawnedByPrefab(this);
                }
                foreach (IStray value in (from s in gameEntity.GetScriptComponents()
                                          where s is IStray
                                          select s).ToArray())
                {
                    StrayEntity[missionObject.GameEntity] = value;
                }
            }
            foreach (IStray value2 in (from s in missionObject.GameEntity.GetScriptComponents()
                                       where s is IStray
                                       select s).ToArray())
            {
                StrayEntity[missionObject.GameEntity] = value2;
            }
        }

        private void DespawnSpawnedPrefab(GameEntity spawnedPrefab)
        {
            spawnedPrefab.Remove(80);
            SpawnedPrefabs.Remove(spawnedPrefab);
            if (StrayEntity.ContainsKey(spawnedPrefab))
            {
                StrayEntity.Remove(spawnedPrefab);
            }
        }

        private void DespawnNearest(Agent userAgent)
        {
            Vec3 spawnerOrigin = SpawningPoint.GetGlobalFrame().origin;
            SpawnedPrefabs.Sort(delegate (GameEntity s, GameEntity s2)
            {
                MatrixFrame globalFrame = s.GetGlobalFrame();
                float num = globalFrame.origin.Distance(spawnerOrigin);
                globalFrame = s2.GetGlobalFrame();
                return num.CompareTo(globalFrame.origin.Distance(spawnerOrigin));
            });
            GameEntity spawnedEntity = SpawnedPrefabs.FirstOrDefault();
            if (spawnedEntity != null)
            {
                Vec3 origin = spawnedEntity.GetGlobalFrame().origin;
                SpawnableItem spawnableItem = SpawnableItems.FirstOrDefault((s) => s.PrefabName == spawnedEntity.Name);
                if (spawnableItem.SpawnerItem != null && origin.Distance(spawnerOrigin) <= spawnableItem.DespawnArea)
                {
                    userAgent.MissionPeer.GetNetworkPeer();
                    DespawnSpawnedPrefab(spawnedEntity);
                }
            }
        }

        public PE_PrefabSpawner() : base(false)
        {
        }

        public string SpawnPointTag = "spawn_point";

        public string SpawningPrefabsXml = "SiegeUnits";

        public Vec3 SpawnOffset;

        public string PrefabSpawnerName = "Siege Unit Deployer";

        public string SpawnerCategoryName = "Siege Units";

        private GameEntity SpawningPoint;
    }
}
