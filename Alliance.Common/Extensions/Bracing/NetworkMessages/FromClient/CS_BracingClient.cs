using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Bracing.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class CS_Bracing : GameNetworkMessage
    {
        public int RequestedAgentIndex { get; private set; }

        public CS_Bracing(int requestedAgentIndex)
        {
            this.RequestedAgentIndex = requestedAgentIndex;
        }

        public CS_Bracing()
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