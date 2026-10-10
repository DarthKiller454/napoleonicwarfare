using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.SapperLogics.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SapperBuildingPointsUpdate : GameNetworkMessage
    {
        public BattleSideEnum TeamSide { get; private set; }
        public int BuildPoints { get; private set; }

        public SapperBuildingPointsUpdate()
        {
        }

        public SapperBuildingPointsUpdate(BattleSideEnum teamSide, int buildPoints)
        {
            TeamSide = teamSide;
            BuildPoints = buildPoints;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket((int)TeamSide, new CompressionInfo.Integer(0, 1, true));
            WriteIntToPacket(BuildPoints, new CompressionInfo.Integer(0, 100000, true));
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            TeamSide = (BattleSideEnum)ReadIntFromPacket(new CompressionInfo.Integer(0, 1, true), ref bufferReadValid);
            BuildPoints = ReadIntFromPacket(new CompressionInfo.Integer(0, 100000, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.None;
        }

        protected override string OnGetLogFormat()
        {
            return "Update sapper building points";
        }
    }
}
