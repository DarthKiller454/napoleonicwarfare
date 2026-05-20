using Alliance.Client.Patch.HarmonyPatch;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch
{
    /// <summary>
    /// This class is responsible for applying Harmony patches and fixing native code in "roundabout" ways.
    /// Must be used as last resort.    
    /// </summary>
    public static class DirtyClientPatcher
    {
        public static bool Patch()
        {
            bool patchSuccess = true;

            patchSuccess &= Patch_MissionNetworkComponent.Patch();
            patchSuccess &= Patch_DefaultAdminPanelOptionProvider.Patch();
            patchSuccess &= Patch_MPArmoryVM.Patch();
            patchSuccess &= Patch_Nickname.Patch();
            patchSuccess &= Patch_MissionAgentLabelView.Patch();
            patchSuccess &= Patch_MPChatLog.Patch();
            patchSuccess &= Patch_MPChatLineVM.Patch();
            patchSuccess &= Patch_RangedSiegeWeaponView_HandleUserAiming.Patch();
            patchSuccess &= Patch_LobbyClient_RequestJoinCustomGame.Patch();
            patchSuccess &= Patch_LobbyClient_OnJoinCustomGameResultMessage.Patch();

            if (patchSuccess) Log(SubModule.ModuleId + " - Patches successful", LogLevel.Information);
            return patchSuccess;
        }
    }
}
