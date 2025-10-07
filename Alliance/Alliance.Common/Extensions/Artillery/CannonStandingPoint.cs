using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Extensions.Artillery
{
    public class CannonStandingPoint : StandingPoint
    {
        public override bool IsDisabledForAgent(Agent agent)
        {
            return !agent.Character.StringId.Contains("artillery") || !agent.IsPlayerControlled || base.IsDisabledForAgent(agent);
        }
    }

    public class AmmoPickUpStandingPoint : StandingPointWithWeaponRequirement
    {
        public override bool IsDisabledForAgent(Agent agent)
        {
            return !agent.Character.StringId.Contains("artillery") || !agent.IsPlayerControlled || base.IsDisabledForAgent(agent);
        }

    }
    public class Cannon12AmmoStandingPoint : StandingPointWithWeaponRequirement
    {
        public string RequiredItemId = "nwf_artillery_shell_cannonball_12pd";
        public string SecondaryItemId = "nwf_artillery_shell_canister";

        private ItemObject _requiredItem;
        private ItemObject _secondaryItem;
        protected override void OnInit()
        {
            base.OnInit();
            _requiredItem = MBObjectManager.Instance.GetObject<ItemObject>(RequiredItemId);
            _secondaryItem = MBObjectManager.Instance.GetObject<ItemObject>(SecondaryItemId);
        }
        public override bool IsDisabledForAgent(Agent agent)
        {

            if (agent?.WieldedWeapon.Item != _requiredItem && agent?.WieldedOffhandWeapon.Item != _requiredItem && agent?.WieldedWeapon.Item != _secondaryItem && agent?.WieldedOffhandWeapon.Item != _secondaryItem || !agent.Character.StringId.Contains("artillery") || !agent.IsPlayerControlled)
            {
                return true;
            }
            if (!IsDeactivated && agent.MountAgent == null && (!IsDisabledForPlayers || agent.IsAIControlled))
            {
                return !agent.IsOnLand();
            }
            return false;

        }

    }
    public class Cannon6AmmoStandingPoint : StandingPointWithWeaponRequirement
    {
        public string RequiredItemId = "nwf_artillery_shell_cannonball_6pd";
        public string SecondaryItemId = "nwf_artillery_shell_canister";

        private ItemObject _requiredItem;
        private ItemObject _secondaryItem;
        protected override void OnInit()
        {
            base.OnInit();
            _requiredItem = MBObjectManager.Instance.GetObject<ItemObject>(RequiredItemId);
            _secondaryItem = MBObjectManager.Instance.GetObject<ItemObject>(SecondaryItemId);
        }
        public override bool IsDisabledForAgent(Agent agent)
        {

            if (agent?.WieldedWeapon.Item != _requiredItem && agent?.WieldedOffhandWeapon.Item != _requiredItem && agent?.WieldedWeapon.Item != _secondaryItem && agent?.WieldedOffhandWeapon.Item != _secondaryItem || !agent.Character.StringId.Contains("artillery") || !agent.IsPlayerControlled)
            {
                return true;
            }
            if (!IsDeactivated && agent.MountAgent == null && (!IsDisabledForPlayers || agent.IsAIControlled))
            {
                return !agent.IsOnLand();
            }
            return false;

        }

    }
    public class Cannon24AmmoStandingPoint : StandingPointWithWeaponRequirement
    {
        public string RequiredItemId = "nwf_artillery_shell_cannonball_24pd";
        public string SecondaryItemId = "nwf_artillery_shell_canister";

        private ItemObject _requiredItem;
        private ItemObject _secondaryItem;
        protected override void OnInit()
        {
            base.OnInit();
            _requiredItem = MBObjectManager.Instance.GetObject<ItemObject>(RequiredItemId);
            _secondaryItem = MBObjectManager.Instance.GetObject<ItemObject>(SecondaryItemId);
        }
        public override bool IsDisabledForAgent(Agent agent)
        {

            if (agent?.WieldedWeapon.Item != _requiredItem && agent?.WieldedOffhandWeapon.Item != _requiredItem && agent?.WieldedWeapon.Item != _secondaryItem && agent?.WieldedOffhandWeapon.Item != _secondaryItem || !agent.Character.StringId.Contains("artillery") || !agent.IsPlayerControlled)
            {
                return true;
            }
            if (!IsDeactivated && agent.MountAgent == null && (!IsDisabledForPlayers || agent.IsAIControlled))
            {
                return !agent.IsOnLand();
            }
            return false;

        }

    }


    public class HowitzerAmmoStandingPoint : StandingPointWithWeaponRequirement
    {
        public string RequiredItemId = "nwf_artillery_shell_howitzer";
        public string SecondaryItemId = "nwf_artillery_shell_canister";

        private ItemObject _requiredItem;
        private ItemObject _secondaryItem;
        protected override void OnInit()
        {
            base.OnInit();
            _requiredItem = MBObjectManager.Instance.GetObject<ItemObject>(RequiredItemId);
            _secondaryItem = MBObjectManager.Instance.GetObject<ItemObject>(SecondaryItemId);
        }
        public override bool IsDisabledForAgent(Agent agent)
        {

            if (agent?.WieldedWeapon.Item != _requiredItem && agent?.WieldedOffhandWeapon.Item != _requiredItem && agent?.WieldedWeapon.Item != _secondaryItem && agent?.WieldedOffhandWeapon.Item != _secondaryItem || !agent.Character.StringId.Contains("artillery") || !agent.IsPlayerControlled)
            {
                return true;
            }
            if (!IsDeactivated && agent.MountAgent == null && (!IsDisabledForPlayers || agent.IsAIControlled))
            {
                return !agent.IsOnLand();
            }
            return false;

        }

    }

    public class MortarAmmoStandingPoint : StandingPointWithWeaponRequirement
    {
        public string RequiredItemId = "nwf_artillery_shell_mortar";

        private ItemObject _requiredItem;
        protected override void OnInit()
        {
            base.OnInit();
            _requiredItem = MBObjectManager.Instance.GetObject<ItemObject>(RequiredItemId);
        }
        public override bool IsDisabledForAgent(Agent agent)
        {
            if (agent?.WieldedWeapon.Item != _requiredItem && agent?.WieldedOffhandWeapon.Item != _requiredItem || !agent.Character.StringId.Contains("artillery") || !agent.IsPlayerControlled)
            {
                return true;
            }
            if (!IsDeactivated && agent.MountAgent == null && (!IsDisabledForPlayers || agent.IsAIControlled))
            {
                return !agent.IsOnLand();
            }
            return false;

        }
    }

    public class CannonFireStandingPoint : StandingPoint
    {
        public string RequiredItemId = "nwf_artillery_cannonlighter";

        private ItemObject _requiredItem;
        public CannonFireStandingPoint()
        {
            AutoSheathWeapons = false;
        }
        protected override void OnInit()
        {
            base.OnInit();
            _requiredItem = MBObjectManager.Instance.GetObject<ItemObject>(RequiredItemId);
        }
        public override bool IsDisabledForAgent(Agent agent)
        {
            if (agent?.WieldedWeapon.Item != _requiredItem && agent?.WieldedOffhandWeapon.Item != _requiredItem || !agent.Character.StringId.Contains("artillery") || !agent.IsPlayerControlled)
            {
                return true;
            }
            if (!IsDeactivated && agent.MountAgent == null && (!IsDisabledForPlayers || agent.IsAIControlled))
            {
                return !agent.IsOnLand();
            }
            return false;

        }
    }
    public class CannonReloadStandingPoint : StandingPoint
    {
        public string RequiredItemId = "nwf_artillery_ramrod";

        private ItemObject _requiredItem;
        public CannonReloadStandingPoint()
        {
            AutoSheathWeapons = false;
        }
        protected override void OnInit()
        {
            base.OnInit();
            _requiredItem = MBObjectManager.Instance.GetObject<ItemObject>(RequiredItemId);
        }
        public override bool IsDisabledForAgent(Agent agent)
        {
            if (agent?.WieldedWeapon.Item != _requiredItem && agent?.WieldedOffhandWeapon.Item != _requiredItem)
            {
                return true;
            }
            if (!IsDeactivated && agent.MountAgent == null && (!IsDisabledForPlayers || agent.IsAIControlled))
            {
                return !agent.IsOnLand();
            }
            return !agent.Character.StringId.Contains("artillery") || base.IsDisabledForAgent(agent) || !agent.IsPlayerControlled;

        }
    }
}
