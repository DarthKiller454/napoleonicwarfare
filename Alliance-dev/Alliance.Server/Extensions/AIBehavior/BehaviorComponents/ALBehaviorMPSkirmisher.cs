using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using Alliance.Server.Extensions.AIBehavior.BehaviorComponents;

namespace Alliance.Server.Extensions.AIBehavior.BehaviorComponents
{
    public class ALBehaviorMPSkirmisher : BehaviorComponent
    {
        private Formation _attachedInfantry;
        public ALBehaviorMPSkirmisher(Formation formation)
            : base(formation)
        {
            CalculateCurrentOrder();
        }

        protected override void CalculateCurrentOrder()
        {
            bool flag = false;
            Formation formation = null;
            float num = float.MaxValue;
            foreach (Team team in Formation.Team.Mission.Teams)
            {
                if (!team.IsEnemyOf(Formation.Team))
                {
                    continue;
                }

                for (int i = 0; i < Math.Min(team.FormationsIncludingSpecialAndEmpty.Count, 8); i++)
                {
                    Formation formation2 = team.FormationsIncludingSpecialAndEmpty[i];
                    if (formation2.CountOfUnits <= 0)
                    {
                        continue;
                    }

                    flag = true;
                    if (formation2.QuerySystem.IsCavalryFormation || formation2.QuerySystem.IsRangedCavalryFormation)
                    {
                        float num2 = formation2.QuerySystem.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition);
                        if (num2 < num)
                        {
                            num = num2;
                            formation = formation2;
                        }
                    }
                }
            }

            if (Formation.Team.FormationsIncludingEmpty.AnyQ((f) => f.CountOfUnits > 0 && f != Formation && f.QuerySystem.IsInfantryFormation))
            {
                _attachedInfantry = TaleWorlds.Core.Extensions.MinBy(Formation.Team.FormationsIncludingEmpty.Where((f) => f.CountOfUnits > 0 && f != Formation && f.QuerySystem.IsInfantryFormation), (f) => f.QuerySystem.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition));
                Formation formation3 = null;
                if (flag)
                {
                    if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition) <= 13000f)
                    {
                        formation3 = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation;
                    }
                    else if (formation != null)
                    {
                        formation3 = formation;
                    }
                }

                Vec2 vec = formation3 == null ? _attachedInfantry.Direction : (formation3.QuerySystem.MedianPosition.AsVec2 - _attachedInfantry.QuerySystem.MedianPosition.AsVec2).Normalized();
                WorldPosition medianPosition = _attachedInfantry.QuerySystem.MedianPosition;
                medianPosition.SetVec2(medianPosition.AsVec2 - vec * ((_attachedInfantry.Depth + Formation.Depth) / 2f));
                CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
            }
            else if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition) <= 13000f)
            {
                Vec2 vec2 = (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2 - Formation.QuerySystem.AveragePosition).Normalized();
                float num3 = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2.Distance(Formation.QuerySystem.AveragePosition);
                WorldPosition medianPosition2 = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition;
                if (num3 > (Formation.QuerySystem.MissileRangeAdjusted * 1.0f))
                {
                    medianPosition2.SetVec2(medianPosition2.AsVec2 - vec2 * (Formation.QuerySystem.MissileRangeAdjusted - Formation.Depth * 0.5f));
                }
                else if (num3 < Formation.QuerySystem.MissileRangeAdjusted * 0.2f)
                {
                    medianPosition2.SetVec2(medianPosition2.AsVec2 - vec2 * (Formation.QuerySystem.MissileRangeAdjusted * 0.2f));
                }
                else
                {
                    medianPosition2.SetVec2(Formation.QuerySystem.AveragePosition);
                }

                CurrentOrder = MovementOrder.MovementOrderMove(medianPosition2);
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec2);
            }
            else
            {
                CurrentOrder = MovementOrder.MovementOrderAdvance;
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            }
        }

        public override void TickOccasionally()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            FormationBehaviorHelper.GetBestTargetFormation(Formation, FormationClass.Ranged);

            var enemyFS = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            if (enemyFS == null)
                return;

            float distance = enemyFS.MedianPosition.AsVec2.Distance(Formation.QuerySystem.AveragePosition);
            float noAmmoRatio = FormationBehaviorHelper.GetOutOfAmmoRatio(Formation);

            if (FormationBehaviorHelper.IsEnemyCavalry(enemyFS.Formation) == true && distance <= 10f)
            {
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderCircle;
                Formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
                Formation.FormOrder = FormOrder.FormOrderDeep;
                Formation.SetMovementOrder(CurrentOrder);
                FormationBehaviorHelper.SetChargeBehaviour(Formation);
            }
            else if (FormationBehaviorHelper.IsEnemyCavalry(enemyFS.Formation) == true && distance <= 50f)
            {
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderCircle;
                Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
                Formation.FormOrder = FormOrder.FormOrderWide;
                Formation.SetMovementOrder(CurrentOrder);
            }
            else if (noAmmoRatio > 0.5f)
            {
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                Formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
                Formation.FormOrder = FormOrder.FormOrderWider;
                Formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                FormationBehaviorHelper.SetChargeBehaviour(Formation);
            }
            else if (distance < 13f)
            {
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                Formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
                Formation.FormOrder = FormOrder.FormOrderWider;
                Formation.SetMovementOrder(CurrentOrder);
                FormationBehaviorHelper.SetChargeBehaviour(Formation);
            }
            else
            {
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
                Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
                Formation.FormOrder = FormOrder.FormOrderWider;
                Formation.SetMovementOrder(CurrentOrder);
            }
        }

        protected override void OnBehaviorActivatedAux()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderWider;
        }

        protected override float GetAiWeight()
        {
            if (Formation.FormationIndex == FormationClass.Ranged | Formation.FormationIndex == FormationClass.General)
            {
                return 1.2f;
            }
            return 0f;
        }
    }
}