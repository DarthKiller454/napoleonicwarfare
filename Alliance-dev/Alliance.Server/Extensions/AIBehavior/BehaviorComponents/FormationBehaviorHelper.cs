using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AIBehavior.BehaviorComponents
{
    public static class FormationBehaviorHelper
    {

        public static float GetOutOfAmmoRatio(Formation formation)
        {
            int totalAgents = 0;
            int agentsOutOfAmmo = 0;

            foreach (var agent in formation.GetUnitsWithoutDetachedOnes())
            {
                if (!agent.IsHuman || !agent.IsActive())
                    continue;

                totalAgents++;
                bool hasBoltAmmo = false;

                for (EquipmentIndex i = EquipmentIndex.Weapon0; i <= EquipmentIndex.Weapon3; i++)
                {
                    MissionWeapon weapon = agent.Equipment[i];
                    if (weapon.IsEmpty || weapon.CurrentUsageItem == null)
                        continue;

                    if (weapon.CurrentUsageItem.WeaponClass == WeaponClass.Bolt | weapon.CurrentUsageItem.WeaponClass == WeaponClass.Arrow && weapon.Amount > 0)
                    {
                        hasBoltAmmo = true;
                        break;
                    }
                }

                if (!hasBoltAmmo)
                {
                    agentsOutOfAmmo++;
                }
            }

            return totalAgents > 0 ? (float)agentsOutOfAmmo / totalAgents : 0f;
        }
        public static void SetChargeBehaviour(Formation formation)
        {
            int totalAgents = 0;

            foreach (var agent in formation.GetUnitsWithoutDetachedOnes())
            {
                if (!agent.IsHuman || !agent.IsActive())
                    continue;

                totalAgents++;

                for (EquipmentIndex i = EquipmentIndex.Weapon0; i <= EquipmentIndex.Weapon3; i++)
                {
                    MissionWeapon weapon = agent.Equipment[i];

                    if (weapon.IsEmpty || weapon.CurrentUsageItem == null)
                        continue;

                    if (weapon.CurrentUsageItem.IsRangedWeapon)
                    {
                        if (weapon.IsReloading)
                        {
                            ActionIndexCache ac = ActionIndexCache.act_none;
                            agent.SetActionChannel(0, ac, true, 0UL, 0.0f, 1f, 0.1f, 0.4f, 0, false, 0.2f, 0, true);
                            agent.SetActionChannel(1, ac, true, 0UL, 0.0f, 1f, 0.1f, 0.4f, 0, false, 0.2f, 0, true);
                        }
                    }
                }
            }
        }
        public static bool IsEnemyCavalry(Formation formation)
        {
            if(formation.QuerySystem.IsCavalryFormation ||
            formation.QuerySystem.IsRangedCavalryFormation ||
                formation.FormationIndex == FormationClass.HeavyCavalry ||
                formation.FormationIndex == FormationClass.LightCavalry)
            {
                return true;
                
            }
            return false;
        }
        public static bool IsEnemySkirmisher(Formation formation)
        {
            if (formation.FormationIndex == FormationClass.Ranged ||
                formation.FormationIndex == FormationClass.Skirmisher)
            {
                return true;

            }
            return false;
        }
        public static bool IsEnemyLine(Formation formation)
        {
            if (formation.FormationIndex == FormationClass.Infantry ||
                formation.FormationIndex == FormationClass.HeavyInfantry)
            {
                return true;

            }
            return false;
        }
        private static readonly FieldInfo _unitSpacingField = typeof(Formation)
        .GetField("_unitSpacing", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void SetUnitSpacing(Formation formation, int newSpacing)
        {
            if (_unitSpacingField != null)
            {
                _unitSpacingField.SetValue(formation, newSpacing);
            }
            else
            {
                throw new MissingFieldException("Could not find '_unitSpacing' field on Formation.");
            }
        }
        public static Formation GetBestTargetFormation(Formation myFormation, FormationClass formationClass)
        {
            var enemyFormations = Mission.Current.Teams
                .Where(t => t.IsEnemyOf(myFormation.Team))
                .SelectMany(t => t.FormationsIncludingEmpty)
                .Where(f => f.CountOfUnits > 0)
                .ToList();

            if (enemyFormations.Count == 0)
                return null;

            var sortedTargets = enemyFormations
                .OrderBy(f =>
                {
                    float score = 0f;

                    bool isCavalry = FormationBehaviorHelper.IsEnemyCavalry(myFormation);
                    bool isHeavyCavalry = myFormation.FormationIndex == FormationClass.HeavyCavalry;
                    bool isGrenadier = myFormation.FormationIndex == FormationClass.HeavyInfantry;
                    bool isInfantry = FormationBehaviorHelper.IsEnemyLine(myFormation);
                    bool isRanged = FormationBehaviorHelper.IsEnemySkirmisher(myFormation);

                    if (isHeavyCavalry) //Heavy Cavalry does a good job at dealing with Line-Infantry type units due to heavy armour and horse resistance
                    {
                        if (FormationBehaviorHelper.IsEnemySkirmisher(f))
                            score -= 8f;
                        else if (FormationBehaviorHelper.IsEnemyCavalry(f))
                            score -= 5f;
                        else if (FormationBehaviorHelper.IsEnemyLine(f))
                            score -= 7f;
                    }
                    else if (isCavalry && !isHeavyCavalry) //Other Cavalry types are more likely to deal with Skirmishers and are on good footing against other cavalry
                    {
                        if (FormationBehaviorHelper.IsEnemySkirmisher(f))
                            score -= 10f;
                        else if (FormationBehaviorHelper.IsEnemyCavalry(f))
                            score -= 9f;
                        else if (FormationBehaviorHelper.IsEnemyLine(f))
                            score -= 7f;
                    }
                    else if (isGrenadier) // Grenadiers love fighting other lines where they can dominate in melee, and won't shy away that much against fighting cavalry vs. Skirmishers
                    {
                        if (FormationBehaviorHelper.IsEnemyLine(f))
                            score -= 10f;
                        else if (FormationBehaviorHelper.IsEnemySkirmisher(f))
                            score -= 7f;
                        else if (FormationBehaviorHelper.IsEnemyCavalry(f))
                            score -= 7f;
                    }
                    else if (isInfantry) // Infantry love fighting other lines aswell but are less shy towards Skirmishers but detest Cavalry as enemies
                    {
                        if (FormationBehaviorHelper.IsEnemyLine(f))
                            score -= 10f;
                        else if (FormationBehaviorHelper.IsEnemySkirmisher(f))
                            score -= 6f;
                        else if (FormationBehaviorHelper.IsEnemyCavalry(f))
                            score -= 5f;
                    }
                    else if (isRanged) // Skirmishers love picking Lines, but wanna avoid Cavalry shooting since they are very maneuverable
                    {
                        if (FormationBehaviorHelper.IsEnemyLine(f))
                            score -= 10f;
                        else if (FormationBehaviorHelper.IsEnemySkirmisher(f))
                            score -= 9f;
                        else if (FormationBehaviorHelper.IsEnemyCavalry(f))
                            score += 6f;
                }

                    // Add distance penalty to prioritize closer targets
                    score += f.QuerySystem.AveragePosition.DistanceSquared(myFormation.QuerySystem.AveragePosition) / 1000f;

                    return score;
                })
                .ToList();
            myFormation.SetTargetFormation(sortedTargets.FirstOrDefault());
            return sortedTargets.FirstOrDefault();
        }
    }
}
