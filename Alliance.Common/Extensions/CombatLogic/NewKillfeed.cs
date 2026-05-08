using Alliance.Common.Core.Configuration.Models;
using NetworkMessages.FromServer;
using System;
using System.Linq;
using System.Security.Principal;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Extensions.CombatLogic
{
    public class KillfeedMissionLogic : MissionLogic
    {

        bool _isGeneralFeedEnabled = BannerlordConfig.KillFeedVisualType< 2;

        public KillfeedMissionLogic()
        {
        }
        public override void OnAgentRemoved(
                 Agent affectedAgent,
                 Agent affectorAgent,
                 AgentState agentState,
                 KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            if (!GameNetwork.IsClient)
                return;
            if (!Config.Instance.KillFeedEnabled || !_isGeneralFeedEnabled || GameNetwork.IsDedicatedServer || !affectedAgent.IsHuman || agentState != AgentState.Killed && agentState != AgentState.Unconscious)
            {
                return;
            }
            if (agentState != AgentState.Killed)
                return;
            if(affectedAgent == affectorAgent || affectedAgent != null && affectorAgent == null)
            {
                ShowSuicideFeed(affectedAgent);
                return;
            }

            if (affectedAgent == null || affectorAgent == null)
                return;

            ShowKillfeed(affectedAgent, affectorAgent, blow);
        }
        private void ShowKillfeed(Agent victim, Agent killer, KillingBlow blow)
        {
            if (victim == null)
                return;

            Agent myAgent = GameNetwork.MyPeer?.ControlledAgent;
            MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
            Team myTeam = missionPeer?.Team;

            Team killerTeam = killer?.Team;
            Team victimTeam = victim.Team;

            bool killerFriendly = killer != null && killerTeam == myTeam;
            bool victimFriendly = victimTeam == myTeam;

            bool iAmKiller = killer != null && killer == myAgent;
            bool iAmVictim = victim == myAgent;

            bool teamKill = killer != null &&
                            killerTeam != null &&
                            victimTeam != null &&
                            killerTeam == victimTeam;

            string killerName = killer?.Name?.ToString() ?? "Environment";
            string victimName = victim.Name?.ToString() ?? "Unknown";

            string weaponText = GetWeaponText(blow);
            bool headshot = IsHeadshot(blow);

            if (headshot)
                weaponText = "Headshot";

            string message = $"{killerName} [{weaponText}] {victimName}";

            Color color;

            if (iAmKiller && !victimFriendly)
            {
                // You killed enemy
                color = Color.FromUint(0xFF4AE8E8); // teal
            }
            else if (!killerFriendly && victimFriendly && !teamKill)
            {
                // Enemy killed ally
                color = Color.FromUint(0xFFE33522); // red
            }
            else if (killerFriendly && !victimFriendly && !teamKill)
            {
                // Ally killed enemy
                color = Color.FromUint(0xFF22B322); // green
            }
            else if (killerFriendly && victimFriendly && teamKill)
            {
                // Ally teamkilled ally
                color = Color.FromUint(0xFF610F0F); // dark red
            }
            else if (!killerFriendly && !victimFriendly && teamKill)
            {
                // Enemy teamkilled enemy
                color = Color.FromUint(0xFFCC34E0); // purple-pink
            }
            else if (iAmVictim)
            {
                color = Color.FromUint(0xFFFA851E); // orange
            }
            else
            {
                color = Color.White;
            }

            InformationManager.DisplayMessage(
                new InformationMessage(message, color)
            );
        }

        private void ShowSuicideFeed(Agent victim)
        {
            if (victim == null)
                return;

            string victimName = victim.Name?.ToString() ?? "Unknown";

            string message = $"{victimName} died";

            Color color;
            color = Color.White;

            InformationManager.DisplayMessage(
                new InformationMessage(message, color)
            );
        }

        private string GetWeaponText(KillingBlow blow)
        {
            WeaponClass weapon = (WeaponClass)blow.WeaponClass;

            switch (weapon)
            {
                case WeaponClass.Dagger:
                    return "Dagger";

                case WeaponClass.OneHandedSword:
                case WeaponClass.TwoHandedSword:
                    return "Sword";

                case WeaponClass.OneHandedAxe:
                case WeaponClass.TwoHandedAxe:
                    return "Axe";

                case WeaponClass.Mace:
                case WeaponClass.TwoHandedMace:
                    return "Mace";

                case WeaponClass.Pick:
                    return "Pick";

                case WeaponClass.OneHandedPolearm:
                case WeaponClass.TwoHandedPolearm:
                case WeaponClass.LowGripPolearm:
                    return "Polearm";

                case WeaponClass.Bow:
                    return "Bow";

                case WeaponClass.Crossbow:
                    return "Musket";

                case WeaponClass.Pistol:
                    return "Pistol";

                case WeaponClass.Musket:
                    return "Musket";

                case WeaponClass.ThrowingAxe:
                    return "Throwing Axe";

                case WeaponClass.ThrowingKnife:
                    return "Throwing Knife";

                case WeaponClass.Javelin:
                    return "Javelin";

                case WeaponClass.Sling:
                case WeaponClass.SlingStone:
                    return "Sling";

                case WeaponClass.Stone:
                    return "Stone";

                case WeaponClass.Arrow:
                    return "Arrow";

                case WeaponClass.Bolt:
                case WeaponClass.Cartridge:
                    return "Shot";

                case WeaponClass.Boulder:
                case WeaponClass.BallistaBoulder:
                    return "Cannonball";

                case WeaponClass.BallistaStone:
                    return "Grapeshot";

                case WeaponClass.SmallShield:
                case WeaponClass.LargeShield:
                    return "Shield Bash";

                case WeaponClass.Banner:
                    return "Banner";

                case WeaponClass.Undefined:
                    return "Unknown";
                case WeaponClass.NumClasses:
                default:
                    return "killed";
            }
        }

        private bool IsHeadshot(KillingBlow blow)
        {
            return blow.IsMissile && blow.VictimBodyPart == BoneBodyPartType.Head;
        }
    }

}