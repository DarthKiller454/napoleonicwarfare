using System;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.ClassFilter;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;
using System.Reflection;
namespace Alliance.Client.Extensions.MPArmory;
public class CustomLobbyClassFilterVM : MPLobbyClassFilterVM
{
    public CustomLobbyClassFilterVM(
        Action<MPLobbyClassFilterClassItemVM, bool> onSelectionChange) // Only passing the selection change delegate here
        : base(onSelectionChange)
    {
        // Remove native factions.
        this.Factions.Clear();

        // Create the delegate for OnFactionFilterChanged using the method you provided.
        Action<MPLobbyClassFilterFactionItemVM, bool> onFactionFilterChanged = CreateOnFactionFilterChangedDelegate();

        // Wrap the factionChangedWrapper to match the expected signature
        Action<MPLobbyClassFilterFactionItemVM> factionChangedWrapper = factionItem =>
            onFactionFilterChanged(factionItem, false);

        // Wrap selectionChangedWrapper to match the expected signature
        Action<MPLobbyClassFilterClassItemVM> selectionChangedWrapper = classItem =>
            onSelectionChange(classItem, false);

        // Add new factions
        this.Factions.Add(new MPLobbyClassFilterFactionItemVM("nwf_austria", true, factionChangedWrapper, selectionChangedWrapper));
        this.Factions.Add(new MPLobbyClassFilterFactionItemVM("nwf_france", true, factionChangedWrapper, selectionChangedWrapper));
        this.Factions.Add(new MPLobbyClassFilterFactionItemVM("nwf_britain", true, factionChangedWrapper, selectionChangedWrapper));
        this.Factions.Add(new MPLobbyClassFilterFactionItemVM("nwf_prussia", true, factionChangedWrapper, selectionChangedWrapper));

        // Mark the first faction as active
        if (this.Factions.Count > 0)
        {
            this.Factions[0].IsActive = true;
        }
        this.RefreshValues();
    }

    private Action<MPLobbyClassFilterFactionItemVM, bool> CreateOnFactionFilterChangedDelegate()
    {
        return (factionItemVm, flag) =>
        {
            this.ActiveClassGroups = factionItemVm.ClassGroups;

            // This part can be invoked from the delegate passed in Postfix
            // We don’t need to call `OnSelectionChange` directly here
        };
    }

    public static CustomLobbyClassFilterVM CreateCustomFilter(MPArmoryVM instance)
    {
        // Fetch the delegate for OnSelectedClassChanged from MPArmoryVM
        var onSelectionChanged = GetOnSelectionChangeDelegate(instance);

        // Create and return the custom filter
        return new CustomLobbyClassFilterVM(onSelectionChanged);
    }
    private static Action<MPLobbyClassFilterClassItemVM, bool> GetOnSelectionChangeDelegate(object instance)
    {
        var method = instance.GetType().GetMethod("OnSelectedClassChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException("Could not find OnSelectedClassChanged in MPArmoryVM");

        return (item, flag) =>
        {
            method.Invoke(instance, new object[] { item, flag });
        };
    }
}