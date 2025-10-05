using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AIBehavior.BehaviorComponents
{
    public class ALBehaviorMPLineInfantry : BehaviorComponent
    {
        private Formation _attachedInfantry;
        private Vec2 _lateralOffset = Vec2.Zero;
        private float _nextOffsetUpdateTime = 0f;
        private const float OffsetInterval = 90f;
        private const float MaxLateralOffset = 32f;
        public ALBehaviorMPLineInfantry(Formation formation)
            : base(formation)
        {
            CalculateCurrentOrder();
        }
        private void UpdateLateralOffsetIfNecessary()
        {
            float currentTime = Mission.Current.CurrentTime;
            if (currentTime >= _nextOffsetUpdateTime)
            {
                _nextOffsetUpdateTime = currentTime + OffsetInterval;

                if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
                {
                    Vec2 enemyPos = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2;

                    float randomAngle = MBRandom.RandomFloat * 2f * TaleWorlds.Library.MathF.PI;

                    float radius = MaxLateralOffset;

                    Vec2 offsetFromEnemy = new Vec2(
                        TaleWorlds.Library.MathF.Cos(randomAngle),
                        TaleWorlds.Library.MathF.Sin(randomAngle)
                    ) * radius;

                    _lateralOffset = (enemyPos + offsetFromEnemy) - Formation.QuerySystem.AveragePosition;
                }
            }
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

            UpdateLateralOffsetIfNecessary();
            if (Formation.Team.FormationsIncludingEmpty.AnyQ((f) => f.CountOfUnits > 0 && f != Formation && f.QuerySystem.IsInfantryFormation))
            {
                _attachedInfantry = TaleWorlds.Core.Extensions.MinBy(Formation.Team.FormationsIncludingEmpty.Where((f) => f.CountOfUnits > 0 && f != Formation && f.QuerySystem.IsInfantryFormation), (f) => f.QuerySystem.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition));
                Formation formation3 = null;
                if (flag)
                {
                    if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition) <= 12000f)
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
            else if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null &&
         Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2
                 .DistanceSquared(Formation.QuerySystem.AveragePosition) <= 12000f &&
         Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2
                 .DistanceSquared(Formation.QuerySystem.AveragePosition) >= 800f)
            {
                Vec2 enemyPos = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2;
                Vec2 ourPos = Formation.QuerySystem.AveragePosition;

                float distanceToTarget = enemyPos.Distance(ourPos);

                float offsetScale = TaleWorlds.Library.MathF.Min(1f, distanceToTarget / 50f);
                Vec2 targetPosition = enemyPos + _lateralOffset * offsetScale;

                float groundZ = Mission.Current.Scene.GetTerrainHeight(targetPosition);
                Vec3 targetPos3 = new Vec3(targetPosition.x, targetPosition.y, groundZ);

                Vec2 directionToEnemy = (enemyPos - targetPosition).Normalized();

                WorldPosition movePos = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, targetPos3, false);

                CurrentOrder = MovementOrder.MovementOrderMove(movePos);
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(directionToEnemy);
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
            FormationBehaviorHelper.GetBestTargetFormation(Formation, FormationClass.Infantry);

            var enemyFS = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            if (enemyFS == null)
                return;

            float distance = enemyFS.MedianPosition.AsVec2.Distance(Formation.QuerySystem.AveragePosition);
            float noAmmoRatio = FormationBehaviorHelper.GetOutOfAmmoRatio(Formation);
            WorldPosition position = Formation.QuerySystem.MedianPosition;
            position.SetVec2(Formation.QuerySystem.AveragePosition);

            if (FormationBehaviorHelper.IsEnemyCavalry(enemyFS.Formation) == true && distance <= 20f)
            {
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderSquare;
                Formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
                Formation.FormOrder = FormOrder.FormOrderDeep;
                Formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
                FormationBehaviorHelper.SetChargeBehaviour(Formation);
            }
            else if (FormationBehaviorHelper.IsEnemyCavalry(enemyFS.Formation) == true && distance <= 50f)
            {
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderSquare;
                Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
                Formation.FormOrder = FormOrder.FormOrderDeep;
                Formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
            }
            else if (noAmmoRatio > 0.3f)
            {
                Formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                Formation.FormOrder = FormOrder.FormOrderDeep;
                Formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                FormationBehaviorHelper.SetChargeBehaviour(Formation);
            }
            else if (distance < 45f)
            {
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                Formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
                Formation.FormOrder = FormOrder.FormOrderDeep;
                Formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                FormationBehaviorHelper.SetChargeBehaviour(Formation);
            }
            else if (distance > (Formation.QuerySystem.MissileRangeAdjusted * 1.1f))
            {
                float capsuleSize = 1.1f;
                float unitsPerRow = 2f;
                float unitSpacing = 1f;
                float flankWidth = capsuleSize * unitsPerRow + unitSpacing;
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                Formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
                Formation.SetMovementOrder(CurrentOrder);
                Formation.FormOrder = FormOrder.FormOrderCustom(flankWidth);
                FormationBehaviorHelper.SetUnitSpacing(Formation, 1);
            }
            else
            {
                float count = Formation.CountOfUnits;
                float capsuleSize = 1.1f;
                float totalUnitWidth = capsuleSize;
                float flankWidth = (count * capsuleSize) / 2;
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
                Formation.FormOrder = FormOrder.FormOrderCustom(flankWidth);
                Formation.SetMovementOrder(CurrentOrder);
                FormationBehaviorHelper.SetUnitSpacing(Formation, 0);
            }
        }



        protected override void OnBehaviorActivatedAux()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderShieldWall;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderDeep;
        }

        protected override float GetAiWeight()
        {
            if (Formation.FormationIndex == FormationClass.Infantry)
            {
                return 1.2f;
            }
            return 0f;
        }
    }
}