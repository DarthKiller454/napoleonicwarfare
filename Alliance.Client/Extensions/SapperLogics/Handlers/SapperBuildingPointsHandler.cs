using Alliance.Common.Extensions;
using Alliance.Common.Extensions.CombatLogic;
using Alliance.Common.Extensions.SapperLogics.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.SapperLogics.Handlers
{
    public sealed class SapperBuildingPointsHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SapperBuildingPointsUpdate>(HandleUpdate);
        }

        private void HandleUpdate(SapperBuildingPointsUpdate message)
        {
            PrefabPlacementMissionLogic missionLogic = Mission.Current?.GetMissionBehavior<PrefabPlacementMissionLogic>();
            missionLogic?.SetClientSapperBuildingPoints(message.TeamSide, message.BuildPoints);
        }
    }
}
