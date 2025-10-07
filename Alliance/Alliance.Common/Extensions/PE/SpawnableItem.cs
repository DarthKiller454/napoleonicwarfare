using System;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Extensions.PE
{
    public struct SpawnableItem
    {
        public SpawnableItem(string prefabName, string spawnerItemId, int maxSpawnAmount, float despawnArea, float adjustPositionX, float adjustPositionY, float adjustPositionZ)
        {
            this.PrefabName = prefabName;
            this.SpawnerItem = MBObjectManager.Instance.GetObject<ItemObject>(spawnerItemId);
            this.MaxSpawnAmount = maxSpawnAmount;
            this.DespawnArea = despawnArea;
            this.AdjustPositionX = adjustPositionX;
            this.AdjustPositionY = adjustPositionY;
            this.AdjustPositionZ = adjustPositionZ;
        }

        public string PrefabName;

        public ItemObject SpawnerItem;

        public int MaxSpawnAmount;

        public float DespawnArea;

        public float AdjustPositionX;

        public float AdjustPositionY;

        public float AdjustPositionZ;
    }
}
