using System;

namespace Alliance.Common.Extensions.PE
{
    public interface ISpawnable
    {
        void OnSpawnedByPrefab(PE_PrefabSpawner spawner);
    }
}
