using Alliance.Common.Extensions;
using Alliance.Common.Extensions.Bracing.NetworkMessages.FromServer;
using Alliance.Common.Extensions.Bracing;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.Bracing.Handlers
{
    public class BracingHandler : IHandlerRegister
    {
        public BracingHandler()
        {
        }

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            //reg.Register<CS_Bracing>(HandleBracing);
        }

        public void HandleBracing(CS_BracingSend message)
        {
            Agent agent = Mission.MissionNetworkHelper.GetAgentFromIndex(message.RequestedAgentIndex); 
        }
    }
}