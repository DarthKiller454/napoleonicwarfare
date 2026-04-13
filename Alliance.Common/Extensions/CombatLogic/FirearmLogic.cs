using NetworkMessages.FromServer;
using System;
using System.Linq;
using System.Security.Principal;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.CombatLogic
{
    public class FirearmsMissionLogic : MissionLogic
    {



        public FirearmsMissionLogic()
        {
        }

        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);
            if (shooterAgent == null || !shooterAgent.IsActive())
                return;

            var wieldedWeapon = shooterAgent.WieldedWeapon;
            if (wieldedWeapon.IsEmpty)
                return;

            var usageItem = wieldedWeapon.CurrentUsageItem;
            if (usageItem == null)
                return;

            var ammoWeapon = wieldedWeapon.AmmoWeapon;
            if (ammoWeapon.IsEmpty || ammoWeapon.Item == null)
                return;

            var frame = new MatrixFrame(orientation, position);
            var offset = (shooterAgent.WieldedWeapon.CurrentUsageItem.WeaponLength + 5) / 100;
            frame.Advance(offset);

            string ammoId = ammoWeapon.Item.StringId;

            if (ammoId == "cartridges")
            {
                Mission.AddParticleSystemBurstByName("musket_effect", frame, true);
            }
            else if (ammoId == "pistol_cartridges")
            {
                Mission.AddParticleSystemBurstByName("pistol_effect", frame, true);
            }
        }

        public override void OnMissileCollisionReaction(
    Mission.MissileCollisionReaction collisionReaction,
    Agent attackerAgent,
    Agent attachedAgent,
    sbyte attachedBoneIndex)
        {
            base.OnMissileCollisionReaction(collisionReaction, attackerAgent, attachedAgent, attachedBoneIndex);

            var missile = Mission.MissilesList.FirstOrDefault(m => m.ShooterAgent == attackerAgent);
            if (missile == null)
                return;

            var item = missile.Weapon.Item;
            if (item == null || !item.StringId.Contains("cannonball") || !item.StringId.Contains("nwf_artillery_shell_howitzer") || !item.StringId.Contains("nwf_artillery_shell_mortar"))
                return;

            if (collisionReaction != Mission.MissileCollisionReaction.BecomeInvisible)
                return;

            if (item.StringId.Contains("cannonball"))
            {
            Vec3 velocity = missile.GetVelocity();
            float speed = velocity.Length;
            if (speed < 4f)
                return;

            Vec3 originalDir = velocity.NormalizedCopy();

            Vec3 missilePos = missile.Entity.GlobalPosition;
            float terrainZ;
            Vec3 terrainNormal;
            Mission.Current.Scene.GetTerrainHeightAndNormal(missilePos.AsVec2, out terrainZ, out terrainNormal);

            float heightDiff = missilePos.z - terrainZ;

            if (heightDiff > 1.0f || heightDiff < -0.5f)
                return;

            bool hitTerrain = missile.Entity?.Parent == null;
            if (!hitTerrain)
                return;

            Vec3 impactDirection = originalDir.NormalizedCopy();

            Vec3 sideDirection = Vec3.CrossProduct(impactDirection, terrainNormal).NormalizedCopy();

            Mat3 craterOrientation = Mat3.Identity;
            craterOrientation.f = impactDirection;
            craterOrientation.s = sideDirection;
            craterOrientation.u = terrainNormal;
            craterOrientation.Orthonormalize();

            Vec3 craterPos = new Vec3(missilePos.x, missilePos.y, terrainZ);

            SpawnCraterAt(craterPos, craterOrientation);

            Vec3 bounceDirection = ReflectVector(originalDir, terrainNormal);

            float steepness = TaleWorlds.Library.MathF.Abs(Vec3.DotProduct(originalDir, terrainNormal));

            float horizontalDamp = 1.0f;
            float verticalDamp = MBMath.Lerp(0.2f, 0.04f, steepness);

            bounceDirection = new Vec3(
                bounceDirection.x * horizontalDamp,
                bounceDirection.y * horizontalDamp,
                bounceDirection.z * verticalDamp
            ).NormalizedCopy();

            float dirDot = Vec3.DotProduct(bounceDirection, originalDir);
            if (dirDot < 0.3f)
                return;

            float speedMultiplier = MBMath.Lerp(0.95f, 0.4f, steepness);
            float newSpeed = speed * speedMultiplier;

            float verticalBiasStrength = MBMath.Lerp(0.0f, 0.68f, 1.0f - dirDot);
            bounceDirection.z -= verticalBiasStrength;
            bounceDirection = bounceDirection.NormalizedCopy();

            Mat3 orientation = Mat3.Identity;
            orientation.f = bounceDirection;
            orientation.Orthonormalize();

            Vec3 spawnPos = missilePos + terrainNormal * 0.07f;

            if (item.StringId.Equals("nwf_artillery_shell_cannonball_6pd"))
                item = Game.Current.ObjectManager.GetObject<ItemObject>("nwf_artillery_shell_cannonball_6pd_bounce");
            if (item.StringId.Equals("nwf_artillery_shell_cannonball_12pd"))
                item = Game.Current.ObjectManager.GetObject<ItemObject>("nwf_artillery_shell_cannonball_12pd_bounce");
            if (item.StringId.Equals("nwf_artillery_shell_cannonball_24pd"))
                item = Game.Current.ObjectManager.GetObject<ItemObject>("nwf_artillery_shell_cannonball_24pd_bounce");

            Mission.Current.AddCustomMissile(
                attackerAgent,
                new MissionWeapon(item, null, attackerAgent.Origin?.Banner, 1),
                spawnPos,
                bounceDirection,
                orientation,
                newSpeed,
                newSpeed,
                addRigidBody: false,
                missile.MissionObjectToIgnore
            );
            }


            if (item.StringId.Contains("nwf_artillery_shell_howitzer") || item.StringId.Contains("nwf_artillery_shell_mortar"))
            {

                Vec3 velocity = missile.GetVelocity();
                float speed = velocity.Length;
                Vec3 missilePos = missile.Entity.GlobalPosition;
                float terrainZ;
                Vec3 terrainNormal;
                Mission.Current.Scene.GetTerrainHeightAndNormal(missilePos.AsVec2, out terrainZ, out terrainNormal);

                float heightDiff = missilePos.z - terrainZ;

                if (heightDiff > 1.0f || heightDiff < -0.5f)
                    return;

                bool hitTerrain = missile.Entity?.Parent == null;
                if (!hitTerrain)
                    return;

                Vec3 spawnPos = missilePos + terrainNormal * 0.07f;

                    item = Game.Current.ObjectManager.GetObject<ItemObject>("nwf_artillery_shell_canister_shot");
                int pelletCount = 30;

                for (int i = 0; i < pelletCount; i++)
                {
                    float angle = MBRandom.RandomFloat * TaleWorlds.Library.MathF.PI * 2f;

                    float radius = MBRandom.RandomFloat;

                    Vec3 tangent = Vec3.CrossProduct(terrainNormal, Vec3.Up);
                    if (tangent.LengthSquared < 0.001f)
                        tangent = Vec3.Side;

                    tangent = tangent.NormalizedCopy();
                    Vec3 bitangent = Vec3.CrossProduct(terrainNormal, tangent).NormalizedCopy();

                    Vec3 dir =
                        tangent * TaleWorlds.Library.MathF.Cos(angle) * radius +
                        bitangent * TaleWorlds.Library.MathF.Sin(angle) * radius +
                        terrainNormal * MBMath.Lerp(0.3f, 1.0f, MBRandom.RandomFloat);

                    dir = dir.NormalizedCopy();

                    // --- SPEED CONTROL ---
                    float speedFactor = MBMath.Lerp(2.0f, 6.0f, MBRandom.RandomFloat);
                    float pelletSpeed = speedFactor;

                    // --- ORIENTATION ---
                    Mat3 orientation = Mat3.Identity;
                    orientation.f = dir;
                    orientation.Orthonormalize();

                    Mission.Current.AddCustomMissile(
                        attackerAgent,
                        new MissionWeapon(item, null, attackerAgent.Origin?.Banner, 1),
                        spawnPos,
                        dir,
                        orientation,
                        pelletSpeed,
                        pelletSpeed,
                        addRigidBody: false,
                        missile.MissionObjectToIgnore
                    );
                }
            }
        }
        public static Vec3 ReflectVector(Vec3 incoming, Vec3 normal)
        {
            normal = normal.NormalizedCopy();
            return incoming - 2f * Vec3.DotProduct(incoming, normal) * normal;
        }
        

        public static Vec3 GetRandomDirection(float deviation, bool fixZ = true)
        {
            float x = MBRandom.RandomFloatRanged(-deviation, deviation);
            var y = MBRandom.RandomFloatRanged(-deviation, deviation);
            var z = fixZ ? 1 : MBRandom.RandomFloatRanged(-deviation, deviation);
            return new Vec3(x, y, z);
        }
        public static Mat3 GetRandomOrientation(Mat3 orientation, float deviation)
        {
            float rand1 = MBRandom.RandomFloatRanged(-deviation, deviation);
            orientation.f.RotateAboutX(rand1);
            float rand2 = MBRandom.RandomFloatRanged(-deviation, deviation);
            orientation.f.RotateAboutY(rand2);
            float rand3 = MBRandom.RandomFloatRanged(-deviation, deviation);
            orientation.f.RotateAboutZ(rand3);
            return orientation;
        }
        public static void SpawnCraterAt(Vec3 position, Mat3 orientation)
        {
            MatrixFrame frame = new MatrixFrame(orientation, position);
            MissionObject prefab = Mission.Current?.CreateMissionObjectFromPrefab("cannonball_crater", frame, entity => {});
        }
    }

}