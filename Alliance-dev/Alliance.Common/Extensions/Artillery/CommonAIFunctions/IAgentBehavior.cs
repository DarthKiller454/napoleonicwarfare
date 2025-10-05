using System.Collections.Generic;

namespace Alliance.Common.Extensions.Artillery.CommonAIFunctions
{
    public interface IAgentBehavior
    {
        void Execute();
        void Terminate();
        List<BehaviorOption> CalculateUtility();
        void SetCurrentTarget(Target target);
    }
}