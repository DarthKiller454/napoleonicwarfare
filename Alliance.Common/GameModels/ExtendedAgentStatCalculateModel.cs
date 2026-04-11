using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.FormationEnforcer.Component;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MPPerkObject;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.GameModels
{
    /// <summary>
    /// GameModel calculating agents stats.
    /// Apply different multiplier on stats depending on agents AI difficulty or player formation.    
    /// </summary>
    public class ExtendedAgentStatCalculateModel : CustomBattleAgentStatCalculateModel
    {

        public ExtendedAgentStatCalculateModel()
        {
        }
        public override float GetMaxCameraZoom(Agent agent)
        {
            if(agent.IsActive())
            {
                var wieldedWeapon = agent.WieldedWeapon;
                if (wieldedWeapon.IsEmpty)
                {
                    return base.GetMaxCameraZoom(agent);
                }
                if (wieldedWeapon.Item.StringId == "nwf_officer_spyglass")
                {
                    return base.GetMaxCameraZoom(agent) * 2f;
                }   
            }
            return base.GetMaxCameraZoom(agent);
        }
    }
}