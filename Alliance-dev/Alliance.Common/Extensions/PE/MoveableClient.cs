using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.PE
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StartMovingBackwardMoveableMachine : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        public StartMovingBackwardMoveableMachine() { }
        public StartMovingBackwardMoveableMachine(MissionObject Object)
        {
            this.Object = Object;
        }
        protected override string OnGetLogFormat()
        {
            return "Start Moving Backward Game Object";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Object.Id);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StartMovingDownMoveableMachine : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        public StartMovingDownMoveableMachine() { }
        public StartMovingDownMoveableMachine(MissionObject Object)
        {
            this.Object = Object;
        }
        protected override string OnGetLogFormat()
        {
            return "Start Moving Down Game Object";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Object.Id);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StartMovingForwardMoveableMachine : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        public StartMovingForwardMoveableMachine() { }
        public StartMovingForwardMoveableMachine(MissionObject Object)
        {
            this.Object = Object;
        }
        protected override string OnGetLogFormat()
        {
            return "Start Moving Forward Game Object";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Object.Id);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StartMovingUpMoveableMachine : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        public StartMovingUpMoveableMachine() { }
        public StartMovingUpMoveableMachine(MissionObject Object)
        {
            this.Object = Object;
        }
        protected override string OnGetLogFormat()
        {
            return "Start Moving Forward Game Object";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Object.Id);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StartTurningLeftMoveableMachine : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        public StartTurningLeftMoveableMachine() { }
        public StartTurningLeftMoveableMachine(MissionObject Object)
        {
            this.Object = Object;
        }
        protected override string OnGetLogFormat()
        {
            return "Start Turning Left Game Object";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Object.Id);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StartTurningRightMoveableMachine : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        public StartTurningRightMoveableMachine() { }
        public StartTurningRightMoveableMachine(MissionObject Object)
        {
            this.Object = Object;
        }
        protected override string OnGetLogFormat()
        {
            return "Start Turning Right Game Object";

        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Object.Id);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StopMovingBackwardMoveableMachine : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        public StopMovingBackwardMoveableMachine() { }
        public StopMovingBackwardMoveableMachine(MissionObject Object)
        {
            this.Object = Object;
        }
        protected override string OnGetLogFormat()
        {
            return "Stop Moving Backward Game Object";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Object.Id);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StopMovingDownMoveableMachine : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        public StopMovingDownMoveableMachine() { }
        public StopMovingDownMoveableMachine(MissionObject Object)
        {
            this.Object = Object;
        }
        protected override string OnGetLogFormat()
        {
            return "Start Moving Down Game Object";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Object.Id);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StopMovingForwardMoveableMachine : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        public StopMovingForwardMoveableMachine() { }
        public StopMovingForwardMoveableMachine(MissionObject Object)
        {
            this.Object = Object;
        }
        protected override string OnGetLogFormat()
        {
            return "Stop Moving Forward Game Object";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Object.Id);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StopMovingUpMoveableMachine : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        public StopMovingUpMoveableMachine() { }
        public StopMovingUpMoveableMachine(MissionObject Object)
        {
            this.Object = Object;
        }
        protected override string OnGetLogFormat()
        {
            return "Stop Moving Up Game Object";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Object.Id);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StopTurningLeftMoveableMachine : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        public StopTurningLeftMoveableMachine() { }
        public StopTurningLeftMoveableMachine(MissionObject Object)
        {
            this.Object = Object;
        }
        protected override string OnGetLogFormat()
        {
            return "Stop Turning Left Game Object";

        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Object.Id);
        }
    }
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StopTurningRightMoveableMachine : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        public StopTurningRightMoveableMachine() { }
        public StopTurningRightMoveableMachine(MissionObject Object)
        {
            this.Object = Object;
        }
        protected override string OnGetLogFormat()
        {
            return "Stop Turning Right Game Object";

        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Object.Id);
        }
    }
}