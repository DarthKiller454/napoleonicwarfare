using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.PE
{
    public class SceneSyncBehavior : MissionNetwork
    {

        public class SyncingTrack
        {
            public NetworkCommunicator peer;
            public int chunkIndex;
            public bool synced;

            public SyncingTrack(NetworkCommunicator peer, int chunkIndex, bool synced)
            {
                this.peer = peer;
                this.chunkIndex = chunkIndex;
                this.synced = synced;
            }
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            this.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
        }
        public override void AfterStart()
        {
            base.AfterStart();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
        }


        private void SyncCarts(NetworkCommunicator peer)
        {
            List<WeakGameEntity> gameEntities = base.Mission.GetActiveEntitiesWithScriptComponentOfType<PE_AttachToAgent>().ToList();
            foreach (WeakGameEntity g in gameEntities)
            {
                PE_AttachToAgent comp = g.GetFirstScriptOfType<PE_AttachToAgent>();
                if (comp.AttachedTo != null)
                {
                    GameNetwork.BeginModuleEventAsServer(peer);
                    GameNetwork.WriteMessage(new SyncAttachToAgent(comp, comp.AttachedTo));
                    GameNetwork.EndModuleEventAsServer();
                }
            }

        }

        protected override void HandleLateNewClientAfterSynchronized(NetworkCommunicator networkPeer)
        {
            base.HandleLateNewClientAfterSynchronized(networkPeer);
            if (networkPeer.IsConnectionActive == false || networkPeer.IsNetworkActive == false) return;
            this.SyncCarts(networkPeer);
        }


        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
            this.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Remove);
        }
        public void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode mode)
        {
            GameNetwork.NetworkMessageHandlerRegisterer networkMessageHandlerRegisterer = new GameNetwork.NetworkMessageHandlerRegisterer(mode);
            if (GameNetwork.IsClient)
            {
                networkMessageHandlerRegisterer.Register<SyncAttachToAgent>(
                    this.HandleSyncAttachToAgentFromServer);

                networkMessageHandlerRegisterer.Register<SyncDetachFromAgent>(
                    this.HandleSyncDetachFromAgentFromServer);
            }
            else
            {
                networkMessageHandlerRegisterer.Register<StartMovingUpMoveableMachine>(this.HandleFromClientStartMovingUpMoveableMachine);
                networkMessageHandlerRegisterer.Register<StartMovingDownMoveableMachine>(this.HandleFromClientStartMovingDownMoveableMachine);
                networkMessageHandlerRegisterer.Register<StartMovingForwardMoveableMachine>(this.HandleFromClientStartMovingForwardMoveableMachine);
                networkMessageHandlerRegisterer.Register<StartMovingBackwardMoveableMachine>(this.HandleFromClientStartMovingBackwardMoveableMachine);
                networkMessageHandlerRegisterer.Register<StartTurningLeftMoveableMachine>(this.HandleFromClientStartTurningLeftMoveableMachine);
                networkMessageHandlerRegisterer.Register<StartTurningRightMoveableMachine>(this.HandleFromClientStartTurningRightMoveableMachine);

                networkMessageHandlerRegisterer.Register<StopMovingUpMoveableMachine>(this.HandleFromClientStopMovingUpMoveableMachine);
                networkMessageHandlerRegisterer.Register<StopMovingDownMoveableMachine>(this.HandleFromClientStopMovingDownMoveableMachine);
                networkMessageHandlerRegisterer.Register<StopMovingForwardMoveableMachine>(this.HandleFromClientStopMovingForwardMoveableMachine);
                networkMessageHandlerRegisterer.Register<StopMovingBackwardMoveableMachine>(this.HandleFromClientStopMovingBackwardMoveableMachine);
                networkMessageHandlerRegisterer.Register<StopTurningLeftMoveableMachine>(this.HandleFromClientStopTurningLeftMoveableMachine);
                networkMessageHandlerRegisterer.Register<StopTurningRightMoveableMachine>(this.HandleFromClientStopTurningRightMoveableMachine);
            }
        }
        private void HandleSyncDetachFromAgentFromServer(SyncDetachFromAgent message)
        {
            if (message.AttachToAgent == null)
                return;

            message.AttachToAgent.SetDetachedFromServer(message.FinalFrame);
        }
        private void HandleSyncAttachToAgentFromServer(SyncAttachToAgent message)
        {
            message.AttachToAgent.SetAttachedAgent(message.UserAgent);
        }

        private bool HandleFromClientStopTurningRightMoveableMachine(NetworkCommunicator player, StopTurningRightMoveableMachine message)
        {
            if (player.ControlledAgent == null) return false;
            ((IMoveable)message.Object).StopTurningRight();
            return true;
        }

        private bool HandleFromClientStopTurningLeftMoveableMachine(NetworkCommunicator player, StopTurningLeftMoveableMachine message)
        {
            if (player.ControlledAgent == null) return false;
            ((IMoveable)message.Object).StopTurningLeft();
            return true;
        }

        private bool HandleFromClientStopMovingBackwardMoveableMachine(NetworkCommunicator player, StopMovingBackwardMoveableMachine message)
        {
            if (player.ControlledAgent == null) return false;
            ((IMoveable)message.Object).StopMovingBackward();
            return true;
        }

        private bool HandleFromClientStopMovingForwardMoveableMachine(NetworkCommunicator player, StopMovingForwardMoveableMachine message)
        {
            if (player.ControlledAgent == null) return false;
            ((IMoveable)message.Object).StopMovingForward();
            return true;
        }

        private bool HandleFromClientStopMovingDownMoveableMachine(NetworkCommunicator player, StopMovingDownMoveableMachine message)
        {
            if (player.ControlledAgent == null) return false;
            ((IMoveable)message.Object).StopMovingDown();
            return true;
        }

        private bool HandleFromClientStopMovingUpMoveableMachine(NetworkCommunicator player, StopMovingUpMoveableMachine message)
        {
            if (player.ControlledAgent == null) return false;
            ((IMoveable)message.Object).StopMovingUp();
            return true;
        }

        private bool HandleFromClientStartTurningRightMoveableMachine(NetworkCommunicator player, StartTurningRightMoveableMachine message)
        {
            if (player.ControlledAgent == null) return false;
            ((IMoveable)message.Object).StartTurningRight();
            return true;
        }

        private bool HandleFromClientStartTurningLeftMoveableMachine(NetworkCommunicator player, StartTurningLeftMoveableMachine message)
        {
            if (player.ControlledAgent == null) return false;
            ((IMoveable)message.Object).StartTurningLeft();
            return true;
        }

        private bool HandleFromClientStartMovingBackwardMoveableMachine(NetworkCommunicator player, StartMovingBackwardMoveableMachine message)
        {
            if (player.ControlledAgent == null) return false;
            ((IMoveable)message.Object).StartMovingBackward();
            return true;
        }

        private bool HandleFromClientStartMovingForwardMoveableMachine(NetworkCommunicator player, StartMovingForwardMoveableMachine message)
        {
            if (player.ControlledAgent == null) return false;
            ((IMoveable)message.Object).StartMovingForward();
            return true;
        }

        private bool HandleFromClientStartMovingDownMoveableMachine(NetworkCommunicator player, StartMovingDownMoveableMachine message)
        {
            if (player.ControlledAgent == null) return false;
            ((IMoveable)message.Object).StartMovingDown();
            return true;
        }

        private bool HandleFromClientStartMovingUpMoveableMachine(NetworkCommunicator player, StartMovingUpMoveableMachine message)
        {
            if (player.ControlledAgent == null) return false;
            ((IMoveable)message.Object).StartMovingUp();
            return true;
        }
    }
}