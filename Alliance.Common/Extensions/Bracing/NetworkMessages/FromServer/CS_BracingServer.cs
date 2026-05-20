using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Bracing.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CS_BracingSend : GameNetworkMessage
    {
        public int RequestedAgentIndex { get; private set; }

        public CS_BracingSend(int requestedAgentIndex)
        {
            this.RequestedAgentIndex = requestedAgentIndex;
        }

        public CS_BracingSend()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            this.RequestedAgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteAgentIndexToPacket(this.RequestedAgentIndex);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Agents;
        }

        protected override string OnGetLogFormat()
        {
            return $"Requesting Brace with id: {RequestedAgentIndex} to brace";
        }
    }
}