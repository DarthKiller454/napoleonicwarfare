using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.Artillery.CommonAIFunctions
{
    public static class TeamExtensions
    {
        public static List<Formation> GetFormations(this Team team)
        {
            return team.FormationsIncludingEmpty.FindAll(form => form.CountOfUnits > 0);
        }

        public static List<Formation> GetFormationsIncludingSpecial(this Team team)
        {
            return team.FormationsIncludingSpecialAndEmpty.FindAll(form => form.CountOfUnits > 0);
        }
    }
}
