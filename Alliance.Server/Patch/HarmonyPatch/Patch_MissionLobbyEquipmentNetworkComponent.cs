using Alliance.Common.GameModes.Battle;
using Alliance.Common.Utilities;
using HarmonyLib;
using NetworkMessages.FromClient;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_MissionLobbyEquipmentNetworkComponent
    {
        private static readonly Harmony Harmony =
            new Harmony(SubModule.ModuleId + nameof(Patch_MissionLobbyEquipmentNetworkComponent));

        private static bool _patched;

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                MethodInfo originalMethod =
                    typeof(MissionLobbyEquipmentNetworkComponent).GetMethod(
                        "HandleClientEventLobbyEquipmentUpdated",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                if (originalMethod == null)
                {
                    Logger.Log(
                        "Alliance - ERROR: Could not find MissionLobbyEquipmentNetworkComponent.HandleClientEventLobbyEquipmentUpdated",
                        Logger.LogLevel.Error);

                    return false;
                }

                MethodInfo prefixMethod =
                    typeof(Patch_MissionLobbyEquipmentNetworkComponent).GetMethod(
                        nameof(Prefix_HandleClientEventLobbyEquipmentUpdated),
                        BindingFlags.Static | BindingFlags.Public);

                Harmony.Patch(
                    originalMethod,
                    prefix: new HarmonyMethod(prefixMethod));

                Logger.Log(
                    "Alliance - Patched MissionLobbyEquipmentNetworkComponent.HandleClientEventLobbyEquipmentUpdated",
                    Logger.LogLevel.Information);


                MethodInfo perkMethod =
    typeof(MissionLobbyEquipmentNetworkComponent).GetMethod(
        "HandleClientEventRequestPerkChange",
        BindingFlags.Instance | BindingFlags.NonPublic);

                if (perkMethod == null)
                {
                    Logger.Log(
                        "Alliance - ERROR: Could not find MissionLobbyEquipmentNetworkComponent.HandleClientEventRequestPerkChange",
                        Logger.LogLevel.Error);

                    return false;
                }

                Harmony.Patch(
                    perkMethod,
                    prefix: new HarmonyMethod(
                        typeof(Patch_MissionLobbyEquipmentNetworkComponent).GetMethod(
                            nameof(Prefix_HandleClientEventRequestPerkChange),
                            BindingFlags.Static | BindingFlags.Public)));

                Logger.Log(
                    "Alliance - Patched MissionLobbyEquipmentNetworkComponent.HandleClientEventRequestPerkChange",
                    Logger.LogLevel.Information);
                return true;
            }
            catch (Exception e)
            {
                Logger.Log(
                    "Alliance - ERROR in " + nameof(Patch_MissionLobbyEquipmentNetworkComponent),
                    Logger.LogLevel.Error);

                Logger.Log(e.ToString(), Logger.LogLevel.Error);

                return false;
            }
        }
        public static bool Prefix_HandleClientEventRequestPerkChange(
    MissionLobbyEquipmentNetworkComponent __instance,
    NetworkCommunicator peer,
    GameNetworkMessage baseMessage,
    ref bool __result)
        {
            try
            {
                if (!GameNetwork.IsServer)
                    return true;

                RequestPerkChange requestPerkChange =
                    baseMessage as RequestPerkChange;

                if (requestPerkChange == null)
                    return true;

                MissionPeer missionPeer = peer?.GetComponent<MissionPeer>();
                if (missionPeer == null)
                    return true;

                SpawnComponent spawnComponent =
                    Mission.Current.GetMissionBehavior<SpawnComponent>();

                if (spawnComponent?.SpawningBehavior is ICustomTroopIndexChangeHandler customHandler)
                {
                    bool handled =
                        customHandler.TryHandlePerkChange(
                            peer,
                            missionPeer,
                            requestPerkChange.PerkListIndex,
                            requestPerkChange.PerkIndex);

                    if (handled)
                    {
                        Logger.Log(
                            "[BattleX Respawn] Perk change handled by custom spawning behavior" +
                            " | Peer=" + missionPeer.Name +
                            " | PerkListIndex=" + requestPerkChange.PerkListIndex +
                            " | PerkIndex=" + requestPerkChange.PerkIndex,
                            Logger.LogLevel.Information);

                        __result = true;

                        /*
                         * Skip native handler.
                         * Native refresh requires AreAgentsSpawning(), which is false
                         * during BattleX respawn selection.
                         */
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Log(
                    "Alliance - ERROR in Prefix_HandleClientEventRequestPerkChange",
                    Logger.LogLevel.Error);

                Logger.Log(e.ToString(), Logger.LogLevel.Error);

                return true;
            }
        }
        public static bool Prefix_HandleClientEventLobbyEquipmentUpdated(
            MissionLobbyEquipmentNetworkComponent __instance,
            NetworkCommunicator peer,
            GameNetworkMessage baseMessage,
            ref bool __result)
        {
            try
            {
                if (!GameNetwork.IsServer)
                    return true;

                RequestTroopIndexChange requestTroopIndexChange =
                    baseMessage as RequestTroopIndexChange;

                if (requestTroopIndexChange == null)
                    return true;

                MissionPeer missionPeer = peer?.GetComponent<MissionPeer>();
                if (missionPeer == null)
                    return true;

                SpawnComponent spawnComponent =
                    Mission.Current.GetMissionBehavior<SpawnComponent>();

                if (spawnComponent?.SpawningBehavior is ICustomTroopIndexChangeHandler customTroopIndexHandler)
                {
                    bool handled =
                        customTroopIndexHandler.TryHandleTroopIndexChange(
                            peer,
                            missionPeer,
                            requestTroopIndexChange.SelectedTroopIndex);

                    if (handled)
                    {
                        Logger.Log(
                            "[BattleX Respawn] Troop index change handled by custom spawning behavior" +
                            " | Peer=" + missionPeer.Name +
                            " | SelectedTroopIndex=" + requestTroopIndexChange.SelectedTroopIndex,
                            Logger.LogLevel.Information);

                        __result = true;

                        /*
                         * Skip native handler.
                         * Native handler requires SpawnComponent.AreAgentsSpawning(),
                         * which is false during BattleX mid-round respawn selection.
                         */
                        return false;
                    }
                }

                /*
                 * Not handled by custom respawn logic.
                 * Let native TaleWorlds logic run normally.
                 */
                return true;
            }
            catch (Exception e)
            {
                Logger.Log(
                    "Alliance - ERROR in Prefix_HandleClientEventLobbyEquipmentUpdated",
                    Logger.LogLevel.Error);

                Logger.Log(e.ToString(), Logger.LogLevel.Error);

                return true;
            }
        }
    }
}