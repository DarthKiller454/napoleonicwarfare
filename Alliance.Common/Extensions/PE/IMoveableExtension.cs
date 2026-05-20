using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using System.Runtime.CompilerServices;
using static TaleWorlds.MountAndBlade.SynchedMissionObject;

namespace Alliance.Common.Extensions.PE
{

    public static class IMoveable_Implementation
    {
        private sealed class MoveableYawState
        {
            public bool HasYawForward;
            public Vec3 YawForward;
            public bool HasExternalFrameControl;

        }
        public static void SetExternalFrameControl(this IMoveable moveable, bool enabled)
        {
            if (moveable == null)
                return;

            MoveableYawState state = GetYawState(moveable);
            state.HasExternalFrameControl = enabled;
        }

        public static bool HasExternalFrameControl(this IMoveable moveable)
        {
            if (moveable == null)
                return false;

            MoveableYawState state = GetYawState(moveable);
            return state.HasExternalFrameControl;
        }
        private static readonly ConditionalWeakTable<object, MoveableYawState> _yawStates =
            new ConditionalWeakTable<object, MoveableYawState>();
        private static MoveableYawState GetYawState(IMoveable moveable)
        {
            object key = moveable.GetAttachedObject();

            MoveableYawState state;

            if (!_yawStates.TryGetValue(key, out state))
            {
                state = new MoveableYawState();
                _yawStates.Add(key, state);
            }

            return state;
        }

        private static Vec3 GetOrInitializeYawForward(IMoveable moveable, MatrixFrame frame)
        {
            MoveableYawState state = GetYawState(moveable);

            if (state.HasYawForward)
                return state.YawForward;

            Vec3 yawForward = frame.rotation.f;
            yawForward.z = 0f;

            if (!NormalizeSafe(ref yawForward))
                yawForward = new Vec3(0f, 1f, 0f);

            state.YawForward = yawForward;
            state.HasYawForward = true;

            return yawForward;
        }

        private static void SetStoredYawForward(IMoveable moveable, Vec3 yawForward)
        {
            if (!NormalizeSafe(ref yawForward))
                yawForward = new Vec3(0f, 1f, 0f);

            MoveableYawState state = GetYawState(moveable);
            state.YawForward = yawForward;
            state.HasYawForward = true;
        }

        private static Vec3 RotateYawForward(Vec3 yawForward, float angle)
        {
            yawForward.z = 0f;

            if (!NormalizeSafe(ref yawForward))
                yawForward = new Vec3(0f, 1f, 0f);

            float cos = TaleWorlds.Library.MathF.Cos(angle);
            float sin = TaleWorlds.Library.MathF.Sin(angle);

            Vec3 rotated = new Vec3(
                yawForward.x * cos - yawForward.y * sin,
                yawForward.x * sin + yawForward.y * cos,
                0f
            );

            if (!NormalizeSafe(ref rotated))
                rotated = yawForward;

            return rotated;
        }

        private static void SetFrameToYawOnly(ref MatrixFrame frame, Vec3 yawForward)
        {
            yawForward.z = 0f;

            if (!NormalizeSafe(ref yawForward))
                yawForward = new Vec3(0f, 1f, 0f);

            Vec3 yawSide = GetYawSide(yawForward);

            frame.rotation.s = yawSide;
            frame.rotation.f = yawForward;
            frame.rotation.u = Vec3.Up;

            frame.rotation.Orthonormalize();
        }
        public static void InitiateMoveSynch(this IMoveable moveable)
        {
            SynchedMissionObject synchObject = moveable.GetAttachedObject().GameEntity.GetFirstScriptOfType<SynchedMissionObject>();
            var prop = typeof(SynchedMissionObject).GetField("_initialSynchFlags", BindingFlags.NonPublic | BindingFlags.Instance);
            SynchedMissionObject.SynchFlags syncFlags = (SynchedMissionObject.SynchFlags)prop.GetValue(synchObject);
            syncFlags |= SynchFlags.SynchTransform;
            prop.SetValue(synchObject, syncFlags);
        }
        public static void SyncCurrentFrame(this IMoveable moveable)
        {
            if (GameNetwork.IsServer)
            {
                MatrixFrame currentFrame = moveable.GetAttachedObject().GameEntity.GetFrame();
                moveable.GetAttachedObject().SetFrameSynched(ref currentFrame, GameNetwork.IsClient);
            }
        }

        public static void RequestMovingUp(this IMoveable moveable)
        {
            if (GameNetwork.IsClient)
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new StartMovingUpMoveableMachine(moveable.GetAttachedObject()));
                GameNetwork.EndModuleEventAsClient();
            }
        }
        public static void RequestMovingDown(this IMoveable moveable)
        {
            if (GameNetwork.IsClient)
            {
                // Send Start Moving Packet Bro.
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new StartMovingDownMoveableMachine(moveable.GetAttachedObject()));
                GameNetwork.EndModuleEventAsClient();
            }
        }
        public static void RequestStopMovingUp(this IMoveable moveable)
        {
            if (GameNetwork.IsClient)
            {
                // Send Start Moving Packet Bro.
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new StopMovingUpMoveableMachine(moveable.GetAttachedObject()));
                GameNetwork.EndModuleEventAsClient();
            }
        }
        public static void RequestStopMovingDown(this IMoveable moveable)
        {
            if (GameNetwork.IsClient)
            {
                // Send Start Moving Packet Bro.
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new StopMovingDownMoveableMachine(moveable.GetAttachedObject()));
                GameNetwork.EndModuleEventAsClient();
            }
        }
        public static void RequestMovingForward(this IMoveable moveable)
        {
            if (GameNetwork.IsClient)
            {
                // Send Start Moving Packet Bro.
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new StartMovingForwardMoveableMachine(moveable.GetAttachedObject()));
                GameNetwork.EndModuleEventAsClient();
            }
        }
        public static void RequestStopMovingForward(this IMoveable moveable)
        {
            if (GameNetwork.IsClient)
            {
                //
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new StopMovingForwardMoveableMachine(moveable.GetAttachedObject()));
                GameNetwork.EndModuleEventAsClient();
            }
        }
        public static void RequestMovingBackward(this IMoveable moveable)
        {
            if (GameNetwork.IsClient)
            {
                //
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new StartMovingBackwardMoveableMachine(moveable.GetAttachedObject()));
                GameNetwork.EndModuleEventAsClient();
            }
        }
        public static void RequestStopMovingBackward(this IMoveable moveable)
        {
            if (GameNetwork.IsClient)
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new StopMovingBackwardMoveableMachine(moveable.GetAttachedObject()));
                GameNetwork.EndModuleEventAsClient();
            }
        }

        public static void RequestTurningLeft(this IMoveable moveable)
        {
            if (GameNetwork.IsClient)
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new StartTurningLeftMoveableMachine(moveable.GetAttachedObject()));
                GameNetwork.EndModuleEventAsClient();
            }
        }
        public static void RequestStopTurningLeft(this IMoveable moveable)
        {
            if (GameNetwork.IsClient)
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new StopTurningLeftMoveableMachine(moveable.GetAttachedObject()));
                GameNetwork.EndModuleEventAsClient();
            }
        }
        public static void RequestTurningRight(this IMoveable moveable)
        {
            if (GameNetwork.IsClient)
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new StartTurningRightMoveableMachine(moveable.GetAttachedObject()));
                GameNetwork.EndModuleEventAsClient();
            }
        }

        public static void RequestStopTurningRight(this IMoveable moveable)
        {
            if (GameNetwork.IsClient)
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new StopTurningRightMoveableMachine(moveable.GetAttachedObject()));
                GameNetwork.EndModuleEventAsClient();
            }
        }

        public static void StartMovingUp(this IMoveable moveable)
        {
            moveable.IsMovingUp = true;

            /*if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new StartMovingUpMoveableMachineServer(moveable.GetAttachedObject(), moveable.GetAttachedObject().GameEntity.GetFrame()));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }*/
            // moveable.SyncCurrentFrame();
        }
        public static void StartMovingDown(this IMoveable moveable)
        {
            moveable.IsMovingDown = true;

            /*if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new StartMovingDownMoveableMachineServer(moveable.GetAttachedObject(), moveable.GetAttachedObject().GameEntity.GetFrame()));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }*/
            // moveable.SyncCurrentFrame();
        }
        public static void StopMovingUp(this IMoveable moveable)
        {
            moveable.IsMovingUp = false;

            /*if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new StopMovingUpMoveableMachineServer(moveable.GetAttachedObject(), moveable.GetAttachedObject().GameEntity.GetFrame()));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }*/
            // moveable.SyncCurrentFrame();
        }
        public static void StopMovingDown(this IMoveable moveable)
        {
            moveable.IsMovingDown = false;

            /*if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new StopMovingDownMoveableMachineServer(moveable.GetAttachedObject(), moveable.GetAttachedObject().GameEntity.GetFrame()));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }*/
            // moveable.SyncCurrentFrame();
        }
        public static void StartMovingForward(this IMoveable moveable)
        {
            moveable.IsMovingForward = true;

            /*if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new StartMovingForwardMoveableMachineServer(moveable.GetAttachedObject(), moveable.GetAttachedObject().GameEntity.GetFrame()));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }*/
            // moveable.SyncCurrentFrame();
        }
        public static void StopMovingForward(this IMoveable moveable)
        {
            moveable.IsMovingForward = false;

            /*if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new StopMovingForwardMoveableMachineServer(moveable.GetAttachedObject(), moveable.GetAttachedObject().GameEntity.GetFrame()));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }*/
            // moveable.SyncCurrentFrame();
        }
        public static void StartMovingBackward(this IMoveable moveable)
        {
            moveable.IsMovingBackward = true;

            /*if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new StartMovingBackwardMoveableMachineServer(moveable.GetAttachedObject(), moveable.GetAttachedObject().GameEntity.GetFrame()));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }*/
        }
        public static void StopMovingBackward(this IMoveable moveable)
        {
            moveable.IsMovingBackward = false;

            /*if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new StopMovingBackwardMoveableMachineServer(moveable.GetAttachedObject(), moveable.GetAttachedObject().GameEntity.GetFrame()));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }*/
            // moveable.SyncCurrentFrame();
        }

        public static void StartTurningLeft(this IMoveable moveable)
        {
            moveable.IsTurningLeft = true;

            /*if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new StartTurningLeftMoveableMachineServer(moveable.GetAttachedObject(), moveable.GetAttachedObject().GameEntity.GetFrame()));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }*/
            // moveable.SyncCurrentFrame();
        }
        public static void StopTurningLeft(this IMoveable moveable)
        {
            moveable.IsTurningLeft = false;

            /*if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new StopTurningLeftMoveableMachineServer(moveable.GetAttachedObject(), moveable.GetAttachedObject().GameEntity.GetFrame()));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }*/
            // moveable.SyncCurrentFrame();
        }
        public static void StartTurningRight(this IMoveable moveable)
        {
            moveable.IsTurningRight = true;

            /*if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new StartTurningRightMoveableMachineServer(moveable.GetAttachedObject(), moveable.GetAttachedObject().GameEntity.GetFrame()));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }*/
            // moveable.SyncCurrentFrame();
        }

        public static void StopTurningRight(this IMoveable moveable)
        {
            moveable.IsTurningRight = false;

            /*if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new StopTurningRightMoveableMachineServer(moveable.GetAttachedObject(), moveable.GetAttachedObject().GameEntity.GetFrame()));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }*/
            // moveable.SyncCurrentFrame();
        }
        public static bool CanGoThere(this IMoveable moveable, MatrixFrame frame)
        {
            return !moveable.GetAttachedObject().GameEntity.CheckPointWithOrientedBoundingBox(frame.origin);
        }
        public static MatrixFrame MoveObjectTick(this IMoveable moveable, float dt)
        {
            MatrixFrame oldFrame = moveable.GetAttachedObject().GameEntity.GetFrame();

            const float maxStepUp = 1.0f;
            const float maxDropDown = 5.5f;
            const float minNormalZ = 0.45f;
            const float originHeightAboveGround = 0.0f;

            bool wantsMoveForward = moveable.IsMovingForward && moveable.GetCanAdvance();
            bool wantsMoveBackward = moveable.IsMovingBackward && moveable.GetCanAdvance();

            bool wantsTurnLeft = moveable.IsTurningLeft && moveable.GetCanRotate();
            bool wantsTurnRight = moveable.IsTurningRight && moveable.GetCanRotate();

            bool wantsMoveUp = moveable.IsMovingUp && moveable.GetCanElevate();
            bool wantsMoveDown = moveable.IsMovingDown && moveable.GetCanElevate();

            bool wantsTranslation =
                wantsMoveForward ||
                wantsMoveBackward ||
                wantsMoveUp ||
                wantsMoveDown;

            // ---------------------------------------------------------
            // 1. Use stored pure yaw, not oldFrame.rotation.f.
            // ---------------------------------------------------------

            Vec3 yawForward = GetOrInitializeYawForward(moveable, oldFrame);

            if (wantsTurnLeft)
                yawForward = RotateYawForward(yawForward, moveable.GetRotationSpeed() * dt);

            if (wantsTurnRight)
                yawForward = RotateYawForward(yawForward, -moveable.GetRotationSpeed() * dt);

            SetStoredYawForward(moveable, yawForward);

            Vec3 yawSide = GetYawSide(yawForward);

            // ---------------------------------------------------------
            // 2. Build a frame from the pure yaw.
            //    Rotation input is now absolute authority.
            // ---------------------------------------------------------

            MatrixFrame acceptedRotationFrame = oldFrame;
            SetFrameToYawOnly(ref acceptedRotationFrame, yawForward);

            // ---------------------------------------------------------
            // 3. Ground-align after yaw.
            //    This may add pitch/roll, but it does not change stored yaw.
            // ---------------------------------------------------------

            CannonGroundSample rotationGround;

            if (TryGetCannonGroundTriangle(
                moveable,
                acceptedRotationFrame,
                yawForward,
                yawSide,
                out rotationGround))
            {
                acceptedRotationFrame.origin.z = rotationGround.OriginZ + originHeightAboveGround;

                AlignFrameToSurfaceWithForcedYaw(
                    ref acceptedRotationFrame,
                    rotationGround.Normal,
                    yawForward
                );
            }

            // If only rotating, return immediately.
            // No terrain check is allowed to cancel the yaw.
            if (!wantsTranslation)
                return acceptedRotationFrame;

            // ---------------------------------------------------------
            // 4. Apply W/S movement along stored yaw.
            //    Do not use MatrixFrame.Advance().
            // ---------------------------------------------------------

            MatrixFrame desiredFrame = acceptedRotationFrame;

            float moveAmount = 0f;

            if (wantsMoveForward)
                moveAmount += moveable.GetAdvanceSpeed() * dt;

            if (wantsMoveBackward)
                moveAmount -= moveable.GetAdvanceSpeed() * dt;

            desiredFrame.origin += yawForward * moveAmount;

            if (wantsMoveUp)
                desiredFrame.Elevate(moveable.GetElevationSpeed() * dt);

            if (wantsMoveDown)
                desiredFrame.Elevate(-moveable.GetElevationSpeed() * dt);

            // ---------------------------------------------------------
            // 5. Validate translation only.
            //    If translation fails, keep the rotation.
            // ---------------------------------------------------------

            CannonGroundSample movementGround;

            if (!TryGetCannonGroundTriangle(
                moveable,
                desiredFrame,
                yawForward,
                yawSide,
                out movementGround))
            {
                return acceptedRotationFrame;
            }

            float heightDelta = movementGround.OriginZ - acceptedRotationFrame.origin.z;

            if (heightDelta > maxStepUp)
                return acceptedRotationFrame;

            if (heightDelta < -maxDropDown)
                return acceptedRotationFrame;

            if (movementGround.Normal.z < minNormalZ)
                return acceptedRotationFrame;

            desiredFrame.origin.z = movementGround.OriginZ + originHeightAboveGround;

            AlignFrameToSurfaceWithForcedYaw(
                ref desiredFrame,
                movementGround.Normal,
                yawForward
            );

            return desiredFrame;
        }
        public static void ResetStoredYawFromCurrentFrame(this IMoveable moveable)
        {
            if (moveable == null)
                return;

            UsableMachine attachedObject = moveable.GetAttachedObject();

            if (attachedObject == null || !attachedObject.GameEntity.IsValid)
                return;

            MatrixFrame frame = attachedObject.GameEntity.GetFrame();

            Vec3 yawForward = frame.rotation.f;
            yawForward.z = 0f;

            if (!NormalizeSafe(ref yawForward))
                yawForward = new Vec3(0f, 1f, 0f);

            SetStoredYawForward(moveable, yawForward);
        }
        private static Vec3 GetYawForward(MatrixFrame frame)
        {
            Vec3 forward = frame.rotation.f;
            forward.z = 0f;

            if (!NormalizeSafe(ref forward))
                forward = new Vec3(0f, 1f, 0f);

            return forward;
        }

        private static void AlignFrameToSurfaceWithForcedYaw(
    ref MatrixFrame frame,
    Vec3 surfaceNormal,
    Vec3 forcedYawForward)
        {
            Vec3 up = surfaceNormal;

            if (!NormalizeSafe(ref up))
                up = Vec3.Up;

            Vec3 yawForward = forcedYawForward;
            yawForward.z = 0f;

            if (!NormalizeSafe(ref yawForward))
                yawForward = new Vec3(0f, 1f, 0f);

            // Project the fixed yaw direction onto the ground plane.
            Vec3 forward = yawForward - up * Dot(yawForward, up);

            if (!NormalizeSafe(ref forward))
                forward = yawForward;

            Vec3 side = Cross(forward, up);

            if (!NormalizeSafe(ref side))
                side = Cross(yawForward, Vec3.Up);

            if (!NormalizeSafe(ref side))
                side = new Vec3(1f, 0f, 0f);

            forward = Cross(up, side);

            if (!NormalizeSafe(ref forward))
                forward = yawForward;

            frame.rotation.s = side;
            frame.rotation.f = forward;
            frame.rotation.u = up;

            frame.rotation.Orthonormalize();
        }

        private static Vec3 GetYawSide(Vec3 yawForward)
        {
            Vec3 side = Cross(yawForward, Vec3.Up);

            if (!NormalizeSafe(ref side))
                side = new Vec3(1f, 0f, 0f);

            return side;
        }
        private static float Dot(Vec3 a, Vec3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        private static Vec3 Cross(Vec3 a, Vec3 b)
        {
            return new Vec3(
                a.y * b.z - a.z * b.y,
                a.z * b.x - a.x * b.z,
                a.x * b.y - a.y * b.x
            );
        }

        private static bool NormalizeSafe(ref Vec3 v)
        {
            float lengthSquared = v.x * v.x + v.y * v.y + v.z * v.z;

            if (lengthSquared < 0.000001f)
                return false;

            float invLength = 1f / TaleWorlds.Library.MathF.Sqrt(lengthSquared);
            v *= invLength;

            return true;
        }
        private static float GetPlaneZAt(Vec3 position, Vec3 planePoint, Vec3 planeNormal)
        {
            if (System.Math.Abs(planeNormal.z) < 0.0001f)
                return position.z;

            return planePoint.z -
                   (planeNormal.x * (position.x - planePoint.x) +
                    planeNormal.y * (position.y - planePoint.y)) / planeNormal.z;
        }
        private static bool TryRaycastGround(
            Scene scene,
            Vec3 position,
            out Vec3 hitPoint)
        {
            hitPoint = new Vec3();

            WeakGameEntity ignoredEntity = default!;

            Vec3 start = position + Vec3.Up * 2.0f;
            Vec3 end = position - Vec3.Up * 5.0f;

            float collisionDistance;

            return scene.RayCastForClosestEntityOrTerrain(
                start,
                end,
                out collisionDistance,
                out hitPoint,
                out ignoredEntity
            );
        }
        private static readonly Vec3 LeftWheelProbeLocal = new Vec3(-1f, -0.25f, -0.015f);
        private static readonly Vec3 RightWheelProbeLocal = new Vec3(1f, -0.25f, -0.015f);
        private static readonly Vec3 RearProbeLocal = new Vec3(0.0f, -2.0f, -0.015f);

        private struct CannonGroundSample
        {
            public float OriginZ;
            public Vec3 Normal;

            public Vec3 LeftWheelHit;
            public Vec3 RightWheelHit;
            public Vec3 RearHit;
        }
        private static bool TryGetCannonGroundTriangle(
    IMoveable moveable,
    MatrixFrame frame,
    Vec3 yawForward,
    Vec3 yawSide,
    out CannonGroundSample sample)
        {
            sample = default;

            Scene scene = moveable.GetAttachedObject().Scene;

            Vec3 leftProbe = TransformLocalPointYawOnly(frame, yawForward, yawSide, LeftWheelProbeLocal);
            Vec3 rightProbe = TransformLocalPointYawOnly(frame, yawForward, yawSide, RightWheelProbeLocal);
            Vec3 rearProbe = TransformLocalPointYawOnly(frame, yawForward, yawSide, RearProbeLocal);

            Vec3 leftHit;
            Vec3 rightHit;
            Vec3 rearHit;

            bool leftHasHit = TryRaycastGround(scene, leftProbe, out leftHit);
            bool rightHasHit = TryRaycastGround(scene, rightProbe, out rightHit);
            bool rearHasHit = TryRaycastGround(scene, rearProbe, out rearHit);

            if (!leftHasHit || !rightHasHit || !rearHasHit)
                return false;

            Vec3 leftToRight = rightHit - leftHit;
            Vec3 leftToRear = rearHit - leftHit;

            Vec3 normal = Cross(leftToRight, leftToRear);

            if (Dot(normal, Vec3.Up) < 0f)
                normal *= -1f;

            if (!NormalizeSafe(ref normal))
                return false;

            if (normal.z < 0.35f)
                return false;

            float originZ = GetPlaneZAt(frame.origin, leftHit, normal);

            sample.LeftWheelHit = leftHit;
            sample.RightWheelHit = rightHit;
            sample.RearHit = rearHit;
            sample.Normal = normal;
            sample.OriginZ = originZ;

            return true;
        }
        private static Vec3 TransformLocalPointYawOnly(
    MatrixFrame frame,
    Vec3 yawForward,
    Vec3 yawSide,
    Vec3 localPoint)
        {
            return frame.origin
                   + yawSide * localPoint.x
                   + yawForward * localPoint.y
                   + Vec3.Up * localPoint.z;
        }
    }
}