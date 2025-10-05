using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.CombatLogic
{
    public class CannonPenetrationLogic : MissionLogic
    {
        public override void OnMissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction, Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex)
        {
            // This method is called when a missile collides with something in the game world like terrain. Makes sure that it penetrates the terrain and spawns a new cannonball advancing towards the terrain.
            base.OnMissileCollisionReaction(collisionReaction, attackerAgent, attachedAgent, attachedBoneIndex);

            var missile = Mission.Missiles.FirstOrDefault(m => m.ShooterAgent == attackerAgent);
            if (missile == null)
                return;

            var item = missile.Weapon.Item;
            if (item == null || !item.StringId.Contains("cannonball"))
                return;

            if (collisionReaction != Mission.MissileCollisionReaction.BecomeInvisible)
                return;

            // Don't penetrate if the cannonball is about to "die"
            Vec3 velocity = missile.GetVelocity();
            float speed = velocity.Length;
            if (speed < 4f)
                return;

            Vec3 missilePos = missile.Entity.GlobalPosition;

            float terrainZ;
            Vec3 terrainNormal;
            Mission.Current.Scene.GetTerrainHeightAndNormal(missilePos.AsVec2, out terrainZ, out terrainNormal);

            bool hitTerrain = missile.Entity?.Parent == null;
            if (!hitTerrain)
                return;

            float heightDiff = missilePos.z - terrainZ;
            // Don't shoot cannonballs through terrain
            if (heightDiff <= 0.05f)
                return;

            if (item.StringId.Equals("nwf_artillery_shell_cannonball_6pd"))
                item = Game.Current.ObjectManager.GetObject<ItemObject>("nwf_artillery_shell_cannonball_6pd_bounce");
            else if (item.StringId.Equals("nwf_artillery_shell_cannonball_12pd"))
                item = Game.Current.ObjectManager.GetObject<ItemObject>("nwf_artillery_shell_cannonball_12pd_bounce");
            else if (item.StringId.Equals("nwf_artillery_shell_cannonball_24pd"))
                item = Game.Current.ObjectManager.GetObject<ItemObject>("nwf_artillery_shell_cannonball_24pd_bounce");

            Vec3 spawnPos = missilePos + velocity.NormalizedCopy() * 0.3f;

            Mission.Current.AddCustomMissile(
                attackerAgent,
                new MissionWeapon(item, null, attackerAgent.Origin?.Banner, 1),
                spawnPos,
                velocity.NormalizedCopy(),
                Mat3.Identity,
                speed,
                speed,
                addRigidBody: false,
                missile.MissionObjectToIgnore
            );
        }
    }
}
