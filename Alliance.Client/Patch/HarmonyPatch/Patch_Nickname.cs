using Alliance.Client.Extensions.CustomName;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Home;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace Alliance.Client.Patch.HarmonyPatch
{
    public static class Patch_Nickname
    {
        private static readonly Harmony Harmony = new(SubModule.ModuleId + nameof(Patch_Nickname));
        private static bool _patched;

        public static bool Patch()
        {
            if (_patched)
                return false;
            _patched = true;

            var cmap = new Type[]
            {
                typeof(LobbyState),
                typeof(Action<BasicCharacterObject>),
                typeof(Action),
                typeof(Action),
                typeof(Action<KeyOptionVM>),
                typeof(Func<string>),
                typeof(Action<bool>)
            };

            var target = typeof(MPLobbyVM).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public,
                null,
                cmap,
                null
            );

            var postfix = typeof(Patch_Nickname).GetMethod(nameof(Postfix),
                BindingFlags.Static | BindingFlags.NonPublic);

            Harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            return true;
        }

        private static void Postfix(
            MPLobbyVM __instance,
            LobbyState lobbyState
        )
        {
            __instance.Home =
                new NWFLobbyHomeVM(
                    lobbyState.NewsManager,
                    new Action<MPLobbyVM.LobbyPage>(page =>
                        __instance.SetPage(page, MPMatchmakingVM.MatchmakingSubPages.Default))
                );

            __instance.RefreshValues();
        }
    }
}