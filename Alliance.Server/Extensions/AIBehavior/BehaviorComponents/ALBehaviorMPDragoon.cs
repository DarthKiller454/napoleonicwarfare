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
    public class ALBehaviorMPDragoon : BehaviorComponent
    {

        public ALBehaviorMPDragoon(Formation formation)
            : base(formation)
        {
            CalculateCurrentOrder();
        }

        private MovementOrder UncapturedFlagMoveOrder()
        {
            return MovementOrder.MovementOrderAdvance;
        }

        protected override void CalculateCurrentOrder()
        {
            if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null || Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition) > 2500f)
            {
                CurrentOrder = UncapturedFlagMoveOrder();
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                return;
            }

            if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsRangedFormation)
            {
                CurrentOrder = MovementOrder.MovementOrderChargeToTarget(Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation);
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                return;
            }

            Vec2 vec = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2 - Formation.QuerySystem.AveragePosition;
            float num = vec.Normalize();
            WorldPosition medianPosition = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition;
            if (num > Formation.QuerySystem.MissileRangeAdjusted)
            {
                medianPosition.SetVec2(medianPosition.AsVec2 - vec * (Formation.QuerySystem.MissileRangeAdjusted - Formation.Depth * 0.5f));
            }
            else if (num < Formation.QuerySystem.MissileRangeAdjusted * 0.4f)
            {
                medianPosition.SetVec2(medianPosition.AsVec2 - vec * (Formation.QuerySystem.MissileRangeAdjusted * 0.7f));
            }
            else
            {
                vec = vec.RightVec();
                medianPosition.SetVec2(Formation.QuerySystem.AveragePosition + vec * 20f);
            }

            CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
            CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
        }

        public override void TickOccasionally()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            FormationBehaviorHelper.GetBestTargetFormation(Formation, FormationClass.HorseArcher);
            if (CurrentOrder.OrderEnum == MovementOrder.MovementOrderEnum.ChargeToTarget && Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsRangedFormation)
            {
                Formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
            }
            else
            {
                Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            }
        }

        protected override void OnBehaviorActivatedAux()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderCustom(2);
        }

        protected override float GetAiWeight()
        {
            if (Formation.FormationIndex == FormationClass.HorseArcher | Formation.FormationIndex == FormationClass.LightCavalry)
            {
                return 1.2f;
            }
            return 0f;
        }
    }
}