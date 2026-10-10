using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.PE
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncAttachToAgent : GameNetworkMessage
    {
        public PE_AttachToAgent AttachToAgent;
        public Agent UserAgent;

        public SyncAttachToAgent() { }
        public SyncAttachToAgent(PE_AttachToAgent attachToAgent, Agent userAgent)
        {
            this.AttachToAgent = attachToAgent;
            this.UserAgent = userAgent;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "Sync Attach To Agent";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.AttachToAgent = (PE_AttachToAgent)Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            this.UserAgent = Mission.MissionNetworkHelper.GetAgentFromIndex(GameNetworkMessage.ReadAgentIndexFromPacket(ref result), true);

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.AttachToAgent.Id);
            GameNetworkMessage.WriteAgentIndexToPacket(this.UserAgent.Index);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncDetachFromAgent : GameNetworkMessage
    {
        public PE_AttachToAgent AttachToAgent;
        public MatrixFrame FinalFrame;

        public SyncDetachFromAgent()
        {
        }

        public SyncDetachFromAgent(PE_AttachToAgent attachToAgent, MatrixFrame finalFrame)
        {
            this.AttachToAgent = attachToAgent;
            this.FinalFrame = finalFrame;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "Sync Detach From Agent";
        }

        protected override bool OnRead()
        {
            bool result = true;

            this.AttachToAgent =
                (PE_AttachToAgent)Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                    GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));

            this.FinalFrame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref result);

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.AttachToAgent.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.FinalFrame);
        }
    }
}