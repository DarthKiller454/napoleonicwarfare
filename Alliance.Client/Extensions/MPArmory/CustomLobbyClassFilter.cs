using System;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.ClassFilter;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory;

namespace Alliance.Client.Extensions.MPArmory
{
    public class CustomLobbyClassFilterVM : MPLobbyClassFilterVM
    {
        private readonly Action<MPLobbyClassFilterClassItemVM, bool> _onSelectionChange;

        private readonly MethodInfo _onFactionFilterChanged;
        private readonly MethodInfo _onSelectionChangeInternal;

        public CustomLobbyClassFilterVM(Action<MPLobbyClassFilterClassItemVM, bool> onSelectionChange)
            : base(onSelectionChange)
        {
            _onSelectionChange = onSelectionChange;

            //
            // Get native private methods once
            //
            _onFactionFilterChanged = typeof(MPLobbyClassFilterVM)
                .GetMethod("OnFactionFilterChanged", BindingFlags.Instance | BindingFlags.NonPublic);

            _onSelectionChangeInternal = typeof(MPLobbyClassFilterVM)
                .GetMethod("OnSelectionChange", BindingFlags.Instance | BindingFlags.NonPublic);

            if (_onFactionFilterChanged == null || _onSelectionChangeInternal == null)
                throw new Exception("Unable to reflect MPLobbyClassFilterVM private methods.");

            //
            // Reset native factions
            //
            Factions.Clear();
            ActiveClassGroups.Clear();

            // Build delegates using the REAL BN1.3 signatures:
            Action<MPLobbyClassFilterFactionItemVM> onFaction = OnFactionChanged_Custom;
            Action<MPLobbyClassFilterClassItemVM> onClass = OnClassChanged_Custom;

            //
            // Add your factions
            //
            Factions.Add(new MPLobbyClassFilterFactionItemVM("nwf_austria", true, onFaction, onClass));
            Factions.Add(new MPLobbyClassFilterFactionItemVM("nwf_france", true, onFaction, onClass));
            Factions.Add(new MPLobbyClassFilterFactionItemVM("nwf_britain", true, onFaction, onClass));
            Factions.Add(new MPLobbyClassFilterFactionItemVM("nwf_prussia", true, onFaction, onClass));
            Factions.Add(new MPLobbyClassFilterFactionItemVM("nwf_russia", true, onFaction, onClass));

            //
            // Activate first faction (same as vanilla)
            //
            if (Factions.Count > 0)
                Factions[0].IsActive = true;

            //
            // Perform correct vanilla initialization via reflection
            //
            _onFactionFilterChanged.Invoke(this, new object[] { Factions[0] });

            RefreshValues();
        }

        // ---------------------------
        // When a faction is clicked
        // ---------------------------
        private void OnFactionChanged_Custom(MPLobbyClassFilterFactionItemVM factionVm)
        {
            _onFactionFilterChanged.Invoke(this, new object[] { factionVm });
        }

        // ---------------------------
        // When a class is clicked
        // ---------------------------
        private void OnClassChanged_Custom(MPLobbyClassFilterClassItemVM classVm)
        {
            _onSelectionChangeInternal.Invoke(this, new object[] { classVm });
        }

        // ---------------------------
        // Factory for Harmony patch
        // ---------------------------
        public static CustomLobbyClassFilterVM CreateCustomFilter(MPArmoryVM armory)
        {
            var del = GetOnSelectedClassChanged(armory);
            return new CustomLobbyClassFilterVM(del);
        }

        private static Action<MPLobbyClassFilterClassItemVM, bool> GetOnSelectedClassChanged(object instance)
        {
            var method = instance.GetType().GetMethod(
                "OnSelectedClassChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return (item, flag) =>
            {
                method.Invoke(instance, new object[] { item, flag });
            };
        }
    }
}