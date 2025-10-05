using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.PE
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class StartMovingBackwardMoveableMachineServer : GameNetworkMessage
    {
        public MissionObject Machine { get; set; }
        public MatrixFrame Frame { get; set; }

        public StartMovingBackwardMoveableMachineServer(MissionObject machine, MatrixFrame frame)
        {
            this.Machine = machine;
            this.Frame = frame;
        }
        public StartMovingBackwardMoveableMachineServer()
        {
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "StartMovingUpMoveableMachineServer";
        }

        protected override bool OnRead()
        {
            bool res = true;
            this.Machine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref res));
            this.Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref res);
            return res;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Machine.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.Frame);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class StartMovingDownMoveableMachineServer : GameNetworkMessage
    {
        public MissionObject Machine { get; set; }
        public MatrixFrame Frame { get; set; }

        public StartMovingDownMoveableMachineServer(MissionObject machine, MatrixFrame frame)
        {
            this.Machine = machine;
            this.Frame = frame;
        }
        public StartMovingDownMoveableMachineServer()
        {
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "StartMovingUpMoveableMachineServer";
        }

        protected override bool OnRead()
        {
            bool res = true;
            this.Machine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref res));
            this.Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref res);
            return res;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Machine.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.Frame);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class StartMovingForwardMoveableMachineServer : GameNetworkMessage
    {
        public MissionObject Machine { get; set; }
        public MatrixFrame Frame { get; private set; }

        public StartMovingForwardMoveableMachineServer(MissionObject machine, MatrixFrame frame)
        {
            this.Machine = machine;
            this.Frame = frame;
        }
        public StartMovingForwardMoveableMachineServer()
        {
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "StartMovingUpMoveableMachineServer";
        }

        protected override bool OnRead()
        {
            bool res = true;
            this.Machine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref res));
            this.Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref res);
            return res;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Machine.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.Frame);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class StartMovingUpMoveableMachineServer : GameNetworkMessage
    {
        public MissionObject Machine { get; set; }
        public MatrixFrame Frame { get; private set; }

        public StartMovingUpMoveableMachineServer(MissionObject machine, MatrixFrame frame)
        {
            this.Machine = machine;
            this.Frame = frame;
        }
        public StartMovingUpMoveableMachineServer()
        {
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "StartMovingUpMoveableMachineServer";
        }

        protected override bool OnRead()
        {
            bool res = true;
            this.Machine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref res));
            this.Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref res);
            return res;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Machine.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.Frame);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class StartTurningLeftMoveableMachineServer : GameNetworkMessage
    {
        public MissionObject Machine { get; set; }
        public MatrixFrame Frame { get; private set; }

        public StartTurningLeftMoveableMachineServer(MissionObject machine, MatrixFrame frame)
        {
            this.Machine = machine;
            this.Frame = frame;
        }
        public StartTurningLeftMoveableMachineServer()
        {
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "StartMovingUpMoveableMachineServer";
        }

        protected override bool OnRead()
        {
            bool res = true;
            this.Machine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref res));
            this.Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref res);
            return res;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Machine.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.Frame);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class StartTurningRightMoveableMachineServer : GameNetworkMessage
    {
        public MissionObject Machine { get; set; }
        public MatrixFrame Frame { get; set; }

        public StartTurningRightMoveableMachineServer(MissionObject machine, MatrixFrame frame)
        {
            this.Machine = machine;
            this.Frame = frame;
        }
        public StartTurningRightMoveableMachineServer()
        {
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "StartMovingUpMoveableMachineServer";
        }

        protected override bool OnRead()
        {
            bool res = true;
            this.Machine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref res));
            this.Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref res);
            return res;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Machine.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.Frame);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class StopMovingBackwardMoveableMachineServer : GameNetworkMessage
    {
        public MissionObject Machine { get; set; }
        public MatrixFrame Frame { get; set; }
        public StopMovingBackwardMoveableMachineServer(MissionObject machine, MatrixFrame frame)
        {
            this.Machine = machine;
            this.Frame = frame;
        }
        public StopMovingBackwardMoveableMachineServer()
        {
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "StartMovingUpMoveableMachineServer";
        }

        protected override bool OnRead()
        {
            bool res = true;
            this.Machine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref res));
            this.Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref res);
            return res;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Machine.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.Frame);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class StopMovingDownMoveableMachineServer : GameNetworkMessage
    {
        public MissionObject Machine { get; set; }
        public MatrixFrame Frame { get; set; }

        public StopMovingDownMoveableMachineServer(MissionObject machine, MatrixFrame frame)
        {
            this.Machine = machine;
            this.Frame = frame;
        }
        public StopMovingDownMoveableMachineServer()
        {
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "StartMovingUpMoveableMachineServer";
        }

        protected override bool OnRead()
        {
            bool res = true;
            this.Machine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref res));
            this.Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref res);
            return res;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Machine.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.Frame);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class StopMovingForwardMoveableMachineServer : GameNetworkMessage
    {
        public MissionObject Machine { get; set; }
        public MatrixFrame Frame { get; private set; }

        public StopMovingForwardMoveableMachineServer(MissionObject machine, MatrixFrame Frame)
        {
            this.Machine = machine;
            this.Frame = Frame;
        }
        public StopMovingForwardMoveableMachineServer()
        {
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "StartMovingUpMoveableMachineServer";
        }

        protected override bool OnRead()
        {
            bool res = true;
            this.Machine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref res));
            this.Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref res);
            return res;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Machine.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.Frame);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class StopMovingUpMoveableMachineServer : GameNetworkMessage
    {
        public MissionObject Machine { get; set; }
        public MatrixFrame Frame { get; set; }
        public StopMovingUpMoveableMachineServer(MissionObject machine, MatrixFrame frame)
        {
            this.Machine = machine;
            this.Frame = frame;
        }
        public StopMovingUpMoveableMachineServer()
        {
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "StartMovingUpMoveableMachineServer";
        }

        protected override bool OnRead()
        {
            bool res = true;
            this.Machine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref res));
            this.Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref res);
            return res;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Machine.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.Frame);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class StopTurningLeftMoveableMachineServer : GameNetworkMessage
    {
        public MissionObject Machine { get; set; }
        public MatrixFrame Frame { get; private set; }

        public StopTurningLeftMoveableMachineServer(MissionObject machine, MatrixFrame frame)
        {
            this.Machine = machine;
            this.Frame = frame;
        }
        public StopTurningLeftMoveableMachineServer()
        {
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "StartMovingUpMoveableMachineServer";
        }

        protected override bool OnRead()
        {
            bool res = true;
            this.Machine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref res));
            this.Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref res);
            return res;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Machine.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.Frame);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class StopTurningRightMoveableMachineServer : GameNetworkMessage
    {
        public MissionObject Machine { get; set; }
        public MatrixFrame Frame { get; private set; }

        public StopTurningRightMoveableMachineServer(MissionObject machine, MatrixFrame frame)
        {
            this.Machine = machine;
            this.Frame = frame;
        }
        public StopTurningRightMoveableMachineServer()
        {
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "StartMovingUpMoveableMachineServer";
        }

        protected override bool OnRead()
        {
            bool res = true;
            this.Machine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref res));
            this.Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref res);
            return res;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Machine.Id);
            GameNetworkMessage.WriteMatrixFrameToPacket(this.Frame);
        }
    }
}