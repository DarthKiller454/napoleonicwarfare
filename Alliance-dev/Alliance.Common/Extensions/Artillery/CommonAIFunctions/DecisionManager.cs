using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;

namespace Alliance.Common.Extensions.Artillery.CommonAIFunctions
{
    public static class DecisionManager
    {
        public static BehaviorOption EvaluateCastingBehaviors(List<IAgentBehavior> behaviors)
        {
            return behaviors
                .SelectMany(behavior => behavior.CalculateUtility())
                .MaxBy(option => option.Target.UtilityValue);
        }
    }
}