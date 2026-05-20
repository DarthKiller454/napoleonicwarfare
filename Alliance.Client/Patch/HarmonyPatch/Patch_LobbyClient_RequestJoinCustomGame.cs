using Alliance.Client.Extensions.ExNativeUI.GameLobby;
using HarmonyLib;
using Messages.FromLobbyServer.ToClient;
using System.Reflection;
using TaleWorlds.MountAndBlade.Diamond;

namespace Alliance.Client.Patch.HarmonyPatch
{
    public static class Patch_LobbyClient_RequestJoinCustomGame
    {
        private static readonly Harmony Harmony =
            new Harmony(SubModule.ModuleId + nameof(Patch_LobbyClient_RequestJoinCustomGame));

        private static bool _patched;

        public static bool Patch()
        {
            if (_patched)
                return false;
            _patched = true;

            MethodInfo target = AccessTools.Method(
                typeof(LobbyClient),
                "RequestJoinCustomGame",
                new[]
                {
                    typeof(CustomBattleId),
                    typeof(string),
                    typeof(bool)
                });

            MethodInfo prefix = AccessTools.Method(
                typeof(Patch_LobbyClient_RequestJoinCustomGame),
                nameof(Prefix));

            Harmony.Patch(target, prefix: new HarmonyMethod(prefix));
            return true;
        }


        private static void Prefix()
        {
            CustomServerJoinCancelState.BeginJoinAttempt();
        }
    }
}