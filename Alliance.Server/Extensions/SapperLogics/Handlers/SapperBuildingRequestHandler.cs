using Alliance.Common.Extensions;
using Alliance.Common.Extensions.SapperLogics.NetworkMessages.FromClient;
using Alliance.Common.Extensions.CombatLogic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.SapperLogics.Handlers
{
    public sealed class SapperBuildingRequestHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<RequestSapperBuilding>(HandleRequest);
        }

        public bool HandleRequest(NetworkCommunicator peer, RequestSapperBuilding request)
        {
            if (!GameNetwork.IsServer || peer == null || request == null)
            {
                return false;
            }

            MissionPeer missionPeer = peer.GetComponent<MissionPeer>();
            Agent builderAgent = missionPeer?.ControlledAgent;

            // The prefab ID is the only client input. The server validates it and derives
            // position/orientation from the peer's currently controlled agent.
            return PrefabPlacementMissionLogic.QueueSapperBuildingRequest(builderAgent, request.PrefabId);
        }
    }
}
