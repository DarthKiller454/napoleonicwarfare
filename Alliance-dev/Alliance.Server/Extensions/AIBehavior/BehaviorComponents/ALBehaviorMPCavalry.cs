using Alliance.Server.GameModes.CaptainX.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace Alliance.Server.Extensions.AIBehavior.BehaviorComponents
{
    public class ALBehaviorMPCavalry : BehaviorComponent
    {
        private bool _hasFlanked = false;

        public ALBehaviorMPCavalry(Formation formation)
            : base(formation)
        {
            CalculateCurrentOrder();
        }

        protected override void CalculateCurrentOrder()
        {
            WorldPosition position = ((base.Formation.AI.Side == FormationAI.BehaviorSide.Right) ? base.Formation.QuerySystem.Team.RightFlankEdgePosition : base.Formation.QuerySystem.Team.LeftFlankEdgePosition);
            Vec2 flankPos = (position.AsVec2 - base.Formation.QuerySystem.AveragePosition).Normalized();
            if (!_hasFlanked)
            {
                CurrentOrder = MovementOrder.MovementOrderMove(position);
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(flankPos);
                return;
            }

            var target = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            
            if (target.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition) <= 1000f)
            {
                _hasFlanked = true;
                CurrentOrder = MovementOrder.MovementOrderMove(target.MedianPosition);
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            }
            else if (target == null || target.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition) <= 1100f)
            {
                _hasFlanked = true;
                CurrentOrder = MovementOrder.MovementOrderChargeToTarget(target.Formation);
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            }
            else
            {
                _hasFlanked = false;
            }
        }

        public override void TickOccasionally()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            FormationBehaviorHelper.GetBestTargetFormation(Formation, FormationClass.Cavalry);
        }

        protected override void OnBehaviorActivatedAux()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
            Formation.FacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderWide;
        }
        protected override float GetAiWeight()
        {
            if (Formation.FormationIndex == FormationClass.Cavalry)
            {
                return 1.2f;
            }
            return 0f;
        }
    }
}