using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Battle
{
    public interface ICustomTroopIndexChangeHandler
    {
        bool TryHandleTroopIndexChange(
            NetworkCommunicator networkPeer,
            MissionPeer missionPeer,
            int selectedTroopIndex);

        bool TryHandlePerkChange(
            NetworkCommunicator networkPeer,
            MissionPeer missionPeer,
            int perkListIndex,
            int perkIndex);
    }
}