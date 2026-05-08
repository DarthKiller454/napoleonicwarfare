using Alliance.Common.Extensions;
using Alliance.Common.Extensions.Bracing.NetworkMessages.FromClient;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Server.Extensions.Vehicles.Handlers
{
    public class BracingHandler : IHandlerRegister
    {
        private static readonly Dictionary<int, DateTime> _swapCooldowns = new Dictionary<int, DateTime>();
        public BracingHandler()
        {
        }
        private static readonly Dictionary<string, string> _weaponSwapMap =
            new Dictionary<string, string>()
        {
            { "austrian_musket_bayonet",          "austrian_musket_bayonet_bracing" },
            { "austrian_musket_bayonet_bracing", "austrian_musket_bayonet" },

            { "french_musket_bayonet",           "french_musket_bayonet_bracing" },
            { "french_musket_bayonet_bracing",   "french_musket_bayonet" },

            { "prussian_musket_bayonet",         "prussian_musket_bayonet_bracing" },
            { "prussian_musket_bayonet_bracing", "prussian_musket_bayonet" },

            { "british_musket_bayonet",          "british_musket_bayonet_bracing" },
            { "british_musket_bayonet_bracing",  "british_musket_bayonet" },

            { "russian_musket_bayonet",          "russian_musket_bayonet_bracing" },
            { "russian_musket_bayonet_bracing",  "russian_musket_bayonet" }
        };


        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            //reg.Register<CS_Bracing>(HandleBracing);
        }

        public bool HandleBracing(NetworkCommunicator peer, CS_Bracing message)
        {
            Agent agent = Mission.MissionNetworkHelper.GetAgentFromIndex(message.RequestedAgentIndex);

            if (agent == null)
                return false;

            if (agent.MissionPeer?.GetNetworkPeer() != peer)
                return false;

            SwapWeapon(agent);

            return true;
        }
        private void SwapWeapon(Agent agent)
        {
            if (agent == null || !agent.IsActive())
                return;

            // -------------------------------------------------
            // 0.25 sec anti-spam cooldown
            // -------------------------------------------------
            int agentId = agent.Index;

            if (_swapCooldowns.TryGetValue(agentId, out DateTime lastUse))
            {
                if ((DateTime.UtcNow - lastUse).TotalSeconds < 0.25f)
                    return;
            }

            _swapCooldowns[agentId] = DateTime.UtcNow;

            EquipmentIndex wieldedSlot = agent.GetPrimaryWieldedItemIndex();

            if (wieldedSlot < EquipmentIndex.WeaponItemBeginSlot ||
                wieldedSlot > EquipmentIndex.Weapon3)
                return;

            MissionWeapon currentWeapon = agent.Equipment[wieldedSlot];

            if (currentWeapon.IsEmpty)
                return;

            ItemObject currentItem = currentWeapon.Item;

            if (currentItem == null)
                return;

            string currentId = currentItem.StringId;

            if (!_weaponSwapMap.TryGetValue(currentId, out string replacementId))
                return;
            ItemObject replacementItem =
                MBObjectManager.Instance.GetObject<ItemObject>(replacementId);

            if (replacementItem == null)
                return;
            MissionWeapon newWeapon = new(
                replacementItem,
                currentWeapon.ItemModifier,
                null
            );
            agent.Equipment[wieldedSlot] = newWeapon;
            agent.TryToWieldWeaponInSlot(
                wieldedSlot,
                Agent.WeaponWieldActionType.Instant,
                false
            );
        }
    }
}