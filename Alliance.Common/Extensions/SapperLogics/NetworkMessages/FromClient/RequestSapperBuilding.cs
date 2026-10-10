using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.SapperLogics.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestSapperBuilding : GameNetworkMessage
    {
        public string PrefabId { get; private set; }

        public RequestSapperBuilding()
        {
        }

        public RequestSapperBuilding(string prefabId)
        {
            PrefabId = prefabId;
        }

        protected override void OnWrite()
        {
            WriteStringToPacket(PrefabId ?? string.Empty);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            PrefabId = ReadStringFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.None;
        }

        protected override string OnGetLogFormat()
        {
            return "Request sapper building prefab";
        }
    }
}
