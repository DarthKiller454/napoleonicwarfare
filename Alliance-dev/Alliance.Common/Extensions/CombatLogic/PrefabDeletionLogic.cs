using Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromServer;
using Alliance.Common.Extensions.UsableEntity.Utilities;
using Helpers;
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
    public class PrefabDeletionLogic : MissionLogic
    {

        public PrefabDeletionLogic()
        {
        }
        public override void OnEndMissionInternal()
        {
            if (Mission.Current?.Scene == null)
                return;

            var allEntitiesWithTag = Mission.Current.Scene.FindEntitiesWithTag("placedbyscript");
            if (allEntitiesWithTag == null)
                return;

            bool isServer = GameNetwork.IsServer;

            foreach (GameEntity entity in allEntitiesWithTag)
            {
                if (entity == null)
                    continue;

                var children = entity.GetChildren();
                if (children != null)
                {
                    foreach (GameEntity childEntity in children)
                    {
                        if (childEntity == null)
                            continue;

                        if (isServer)
                        {
                            GameNetwork.BeginBroadcastModuleEvent();
                            GameNetwork.WriteMessage(new RemoveEntity(childEntity.GlobalPosition));
                            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                        }

                        childEntity.Remove(0);
                    }
                }

                if (entity.HasTag("placedbyscript"))
                {
                    if (isServer)
                    {
                        GameNetwork.BeginBroadcastModuleEvent();
                        GameNetwork.WriteMessage(new RemoveEntity(entity.GlobalPosition));
                        GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                    }

                    entity.Remove(0);
                }
            }
            foreach (Agent agent in Mission.Current.Agents)
            {
                if (agent == null || !agent.IsActive() || !agent.IsHuman)
                    continue;

                if (agent.IsUsingGameObject)
                {
                    GameEntity usedEntity = agent.CurrentlyUsedGameObject.GameEntity;

                    if (usedEntity == null || usedEntity.Scene == null)
                    {
                        agent.StopUsingGameObject();
                    }
                }
            }
        }

    }
}