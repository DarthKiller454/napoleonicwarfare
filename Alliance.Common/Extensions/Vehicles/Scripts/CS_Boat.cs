using Alliance.Common.Extensions.Vehicles.NetworkMessages.FromClient;
using Alliance.Common.Extensions.Vehicles.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.Vehicles.Scripts
{
    public class CS_Boat : CS_Vehicle
    {
        private float baseBoatLevel;
        private float maxDefaultSpeed = 5f;
        private BoatPosition boatStatus = BoatPosition.IN_WATER;
        private MatrixFrame starterPosition;
        private HashSet<GameEntity> _ownEntities; 

        public CS_Boat()
        {
            MaxDownwardSpeed = 0f;
            MaxUpwardSpeed = 0f;
            MaxTurnAngle = 10;
            ForceDecelerate = true;
            CanFly = false;
        }

        void ResetPosition(object sender, PropertyChangedEventArgs e)
        {
            SyncFrame(starterPosition);
        }

        protected override void OnInit()
        {
            base.OnInit();
            starterPosition = GameEntity.GetFrame();
            Mission.Current.OnMissionReset += ResetPosition;
            baseBoatLevel = FollowsTerrainPoints.First().GlobalPosition.Z; 
            _ownEntities = new HashSet<GameEntity>();
            List<GameEntity> children = new List<GameEntity>();
            GameEntity.GetChildrenRecursive(ref children);

            _ownEntities.Add(GameEntity); // add root
            foreach (var child in children)
            {
                _ownEntities.Add(child);
            }
        }
        protected override void OnTick(float dt)
        {
            MatrixFrame frame = GameEntity.GetGlobalFrame();
            if (GameNetwork.IsServer)
            {
                if (_brakeCooldown <= 0f && CheckAndBrakeIfObstacle())
                {
                    SetGlobalFrameSynched(ref frame, false);
                    ServerApplyBrake();
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new CS_VehicleSyncBrake(Id));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                }
            }
            if (GameNetwork.IsClient)
            {
                if (_brakeCooldown <= 0f && CheckAndBrakeIfObstacle())
                {
                    ServerApplyBrake();
                }
            }


            // (Status check logic...)
            if (frame.origin.Z > baseBoatLevel + 0.09f)
            {
                boatStatus = BoatPosition.FRONT_LANDED;
            }
            else if (frame.origin.Z <= baseBoatLevel + 0.07f)
            {
                boatStatus = BoatPosition.IN_WATER;
            }
            UpdateBoatOnStatus();
            base.OnTick(dt);
        }
        private void UpdateBoatOnStatus()
        {
            switch (boatStatus)
            {
                case BoatPosition.FRONT_LANDED:
                    UpdateSpeedIfNeeded(true);
                    break;
                case BoatPosition.BACK_LANDED:
                    UpdateSpeedIfNeeded(false);
                    break;
                case BoatPosition.IN_WATER:
                    GetMaxForwardSpeed = maxDefaultSpeed;
                    GetMaxBackwardSpeed = maxDefaultSpeed;
                    DecelerationRate = 0.5f;
                    break;
                default:
                    break;
            }
        }

        private void UpdateSpeedIfNeeded(bool v)
        {
            CurrentTurnRate = 0f;
            if (GetMaxForwardSpeed == 0)
            {
                DecelerationRate = 8f;
            }

            if (v)
            {
                GetMaxForwardSpeed -= 1;
                GetMaxBackwardSpeed += 1;
            }
            else
            {
                GetMaxForwardSpeed += 1;
                GetMaxBackwardSpeed -= 1;
            }
        }
        public override void UpdatePositionAndRotation(float dt, ref MatrixFrame frame)
        {
            bool moving = CurrentForwardSpeed != 0f || CurrentUpwardSpeed != 0f || CurrentTurnRate != 0f;

            if (CurrentTurnRate != 0f)
            {
                frame.Rotate((float)(CurrentTurnRate * (Math.PI / 180) * dt), Vec3.Up);
            }

            if (CurrentForwardSpeed != 0f)
            {
                frame.Advance(CurrentForwardSpeed * dt);
            }

            if (CurrentUpwardSpeed != 0f)
            {
                float elevation = CurrentUpwardSpeed * dt;
                frame.Elevate(elevation);
                FlyElevation += elevation;
                if (FlyElevation < 0) FlyElevation = 0;
            }

            if (boatStatus == BoatPosition.IN_WATER)
            {
                frame.origin.z = baseBoatLevel;
            }
            if (FollowTerrain && moving && frame.origin.Z > baseBoatLevel + 0.09f)
            {
                Vec3[] collisionPoints = GetCollisionPoints();

                AlignFrameWithGround(ref frame, GameEntity, collisionPoints);

                AdjustPositionToTerrain(ref frame, collisionPoints);
            }

            _lastAgentSync += dt;
            if (_lastAgentSync > 1f)
            {
                MovePilotAndPassengers();
                _lastAgentSync = 0f;
            }
        }
        public override void UpdateDirectionAndSpeed(float dt)
        {
            if (_brakeCooldown > 0f)
            {
                _brakeCooldown -= dt;
                _brakeCooldown = Math.Max(_brakeCooldown, 0);

                CurrentForwardSpeed = 0f;
                CurrentUpwardSpeed = 0f;
                CurrentTurnRate = 0f;

                _moveForward = false;
                _moveBackward = false;
                _moveUpward = false;
                _moveDownward = false;
                _turnLeft = false;
                _turnRight = false;
                return;
            }

            if (CheckAndBrakeIfObstacle())
                return;
            if (PilotAgent == null)
            {
                Decelerate(dt);
                DecelerateVertical(dt);
                SlowdownTurn(dt);
                return;
            }
            if (_moveForward) MoveForward(dt);
            else if (_moveBackward) MoveBackward(dt);
            else Decelerate(dt);

            if (_moveUpward) MoveUpward(dt);
            else if (_moveDownward) MoveDownward(dt);
            else DecelerateVertical(dt);

            if (_turnLeft) TurnLeft(dt);
            else if (_turnRight) TurnRight(dt);
            else SlowdownTurn(dt);
        }
        private bool CheckAndBrakeIfObstacle(float distance = 4f, float height = 1f)
        {

            Vec3 origin = GameEntity.GlobalPosition;
            Vec3 direction = GameEntity.GetGlobalFrame().rotation.f;

            Vec3 rayStart = origin + new Vec3(0, 0, height);
            Vec3 rayEnd = rayStart + direction * distance;

            float collisionDistance;
            Vec3 hitPoint;
            GameEntity hitEntity;

            bool hit = Mission.Current.Scene.RayCastForClosestEntityOrTerrain(
                rayStart,
                rayEnd,
                out collisionDistance,
                out hitPoint,
                out hitEntity,
                0.05f,
                BodyFlags.CommonCollisionExcludeFlags);

            if (hit && hitEntity == null)
            {
                    ApplyEmergencyBrake();
                    return true;
            }

            if (hit && hitEntity != null && hitEntity.Pointer != UIntPtr.Zero)
            {
                if (!_ownEntities.Contains(hitEntity) && !hitEntity.HasTag("water"))
                {
                    ApplyEmergencyBrake();
                    return true;
                }
            }

            return false;
        }
        private enum BoatPosition
        {
            IN_WATER,
            FRONT_LANDED,
            BACK_LANDED
        }
    }
}
