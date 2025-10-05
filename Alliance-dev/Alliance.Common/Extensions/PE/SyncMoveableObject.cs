
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.PE
{
    public class SyncMoveableArty : MissionNetwork
    {

        public Queue<SyncingTrack> peerSyncDestructableHitPointsQueue = new Queue<SyncingTrack>();
        public Queue<SyncingTrack> peerSyncItemGatheringQueue = new Queue<SyncingTrack>();
        public Queue<SyncingTrack> peerSyncDestructableWithItemsQueue = new Queue<SyncingTrack>();

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

        public static List<List<T>> ChunkList<T>(int chunkSize, List<T> list)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentException("Chunk size must be greater than zero.");
            }

            List<List<T>> result = new List<List<T>>();

            for (int i = 0; i < list.Count; i += chunkSize)
            {
                List<T> chunk = list.Skip(i).Take(chunkSize).ToList();
                result.Add(chunk);
            }

            return result;
        }
        public int RepairTimeoutAfterHit = 5 * 60;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            this.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
        }

        public override void AfterStart()
        {
            base.AfterStart();
        }

        // private void SyncAttachableObjects()
        

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
        }

        private void ProcessSyncQueue(Queue<SyncingTrack> queue, string queueName)
        {
            if (queue.Count == 0) return;
            SyncingTrack syncingTrack = queue.Peek();
            if (syncingTrack.peer.IsConnectionActive == false)
            {
                queue.Dequeue();
                return;
            }
        }

        private void SyncMoveableObject(NetworkCommunicator peer)
        {
            List<GameEntity> gameEntities = base.Mission.GetActiveEntitiesWithScriptComponentOfType<PE_MoveableMachine>().ToList();
            foreach (GameEntity g in gameEntities)
            {
                PE_MoveableMachine moveableMachine = g.GetFirstScriptOfType<PE_MoveableMachine>();
                if (moveableMachine.IsMovingBackward)
                {
                    GameNetwork.BeginModuleEventAsServer(peer);
                    GameNetwork.WriteMessage(new StartMovingBackwardMoveableMachineServer(moveableMachine, moveableMachine.GameEntity.GetFrame()));
                    GameNetwork.EndModuleEventAsServer();
                }
                if (moveableMachine.IsMovingForward)
                {
                    GameNetwork.BeginModuleEventAsServer(peer);
                    GameNetwork.WriteMessage(new StartMovingForwardMoveableMachineServer(moveableMachine, moveableMachine.GameEntity.GetFrame()));
                    GameNetwork.EndModuleEventAsServer();
                }
                if (moveableMachine.IsMovingDown)
                {
                    GameNetwork.BeginModuleEventAsServer(peer);
                    GameNetwork.WriteMessage(new StartMovingDownMoveableMachineServer(moveableMachine, moveableMachine.GameEntity.GetFrame()));
                    GameNetwork.EndModuleEventAsServer();
                }
                if (moveableMachine.IsMovingUp)
                {
                    GameNetwork.BeginModuleEventAsServer(peer);
                    GameNetwork.WriteMessage(new StartMovingUpMoveableMachineServer(moveableMachine, moveableMachine.GameEntity.GetFrame()));
                    GameNetwork.EndModuleEventAsServer();
                }
                if (moveableMachine.IsTurningLeft)
                {
                    GameNetwork.BeginModuleEventAsServer(peer);
                    GameNetwork.WriteMessage(new StartTurningLeftMoveableMachineServer(moveableMachine, moveableMachine.GameEntity.GetFrame()));
                    GameNetwork.EndModuleEventAsServer();
                }
                if (moveableMachine.IsTurningRight)
                {
                    GameNetwork.BeginModuleEventAsServer(peer);
                    GameNetwork.WriteMessage(new StartTurningRightMoveableMachineServer(moveableMachine, moveableMachine.GameEntity.GetFrame()));
                    GameNetwork.EndModuleEventAsServer();
                }
            }
        }

        protected override void HandleLateNewClientAfterSynchronized(NetworkCommunicator networkPeer)
        {
            base.HandleLateNewClientAfterSynchronized(networkPeer);
            if (networkPeer.IsConnectionActive == false || networkPeer.IsNetworkActive == false) return;
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