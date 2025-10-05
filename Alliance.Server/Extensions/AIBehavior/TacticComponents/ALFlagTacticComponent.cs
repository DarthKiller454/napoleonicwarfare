using Alliance.Server.Extensions.AIBehavior.BehaviorComponents;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AIBehavior.TacticComponents
{
    public class ALFlagTacticComponent : TacticComponent
    {
        public ALFlagTacticComponent(Team team) : base(team)
        {
        }

        protected override void TickOccasionally()
        {
            foreach (Formation item in FormationsIncludingEmpty)
            {
                if (item.CountOfUnits > 0)
                {
                    item.AI.ResetBehaviorWeights();
                    item.AI.SetBehaviorWeight<BehaviorCharge>(1f);
                    item.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);

                    item.AI.SetBehaviorWeight<ALBehaviorMPLineInfantry>(1f);
                    item.AI.SetBehaviorWeight<ALBehaviorMPLightInfantry>(1f);
                    item.AI.SetBehaviorWeight<ALBehaviorMPGrenadier>(1f);
                    item.AI.SetBehaviorWeight<ALBehaviorMPSkirmisher>(1f);
                    item.AI.SetBehaviorWeight<ALBehaviorMPDragoon>(1f);
                    item.AI.SetBehaviorWeight<ALBehaviorMPCavalry>(1f);
                    item.AI.SetBehaviorWeight<ALBehaviorMPHeavyCavalry>(1f);
                }
            }
        }
    }
}