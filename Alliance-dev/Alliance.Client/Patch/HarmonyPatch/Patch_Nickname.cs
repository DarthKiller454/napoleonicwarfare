using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using Alliance.Client.Extensions.CustomName;

namespace Alliance.Client.Patch.HarmonyPatch
{
    public static class Patch_Nickname
    {
        private static readonly Harmony Harmony = new Harmony("Patch_Nickname");
        private static bool _patched;

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                var constructorParams = new Type[]
                {
                    typeof(LobbyState),
                    typeof(Action<BasicCharacterObject>),
                    typeof(Action),
                    typeof(Action<KeyOptionVM>),
                    typeof(Func<string>),
                    typeof(Action<bool>)
                };

                var target = typeof(MPLobbyVM).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, constructorParams, null);

                if (target == null)
                {
                    // Optionally log failure
                    return false;
                }

                var postfix = typeof(Patch_Nickname).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic);
                Harmony.Patch(target, postfix: new HarmonyMethod(postfix));

                return true;
            }
            catch (Exception e)
            {
                // Optionally log exception
                return false;
            }
        }

        private static void Postfix(MPLobbyVM __instance, LobbyState lobbyState)
        {
            _curInstance = __instance;
            __instance.Home = new NWFLobbyHomeVM(lobbyState.NewsManager, new Action<MPLobbyVM.LobbyPage>(OnChangePageRequest));
            __instance.RefreshValues();
        }

        private static void OnChangePageRequest(MPLobbyVM.LobbyPage page)
        {
            _curInstance?.SetPage(page, MPMatchmakingVM.MatchmakingSubPages.Default);
        }

        private static MPLobbyVM _curInstance;
    }
}