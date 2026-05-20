using System.Reflection;
using Alliance.Client.Extensions.ExNativeUI.GameLobby;
using HarmonyLib;
using Messages.FromLobbyServer.ToClient;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;

namespace Alliance.Client.Patch.HarmonyPatch
{
    public static class Patch_LobbyClient_OnJoinCustomGameResultMessage
    {
        private static readonly Harmony Harmony =
            new Harmony(SubModule.ModuleId + nameof(Patch_LobbyClient_OnJoinCustomGameResultMessage));

        private static bool _patched;

        public static bool Patch()
        {
            if (_patched)
                return false;

            _patched = true;

            MethodInfo target = AccessTools.Method(
                typeof(LobbyClient),
                "OnJoinCustomGameResultMessage",
                new[]
                {
                    typeof(JoinCustomGameResultMessage)
                });

            MethodInfo prefix = AccessTools.Method(
                typeof(Patch_LobbyClient_OnJoinCustomGameResultMessage),
                nameof(OnJoinCustomGameResultMessagePrefix));

            if (target == null || prefix == null)
                return false;

            Harmony.Patch(target, prefix: new HarmonyMethod(prefix));

            return true;
        }

        private static bool OnJoinCustomGameResultMessagePrefix(
            LobbyClient __instance,
            JoinCustomGameResultMessage message)
        {
            if (message == null)
                return true;

            // Case 1:
            // ESC was pressed during the join attempt.
            // Block a late successful join response from continuing the connection.
            if (CustomServerJoinCancelState.IsCancelRequested())
            {
                try
                {
                    __instance.QuitFromCustomGame();
                    __instance.Logout(new TextObject("{=CustomGameJoin_Cancelled}Connection Cancelled."));
                }
                catch
                {
                }

                CustomServerJoinCancelState.FinishCancelledJoin();

                return false;
            }

            // Case 2:
            // TW lobby says the previous request is still pending.
            // Their original handling only shows the message and returns,
            // so we force a local cleanup here.
            if (!message.Success &&
                message.Response == CustomGameJoinResponse.AlreadyRequestedWaitingForServerResponse)
            {
                try
                {
                    __instance.QuitFromCustomGame();
                    __instance.Logout(new TextObject("{=CustomGameJoin_Cancelled}Connection Cancelled."));
                }
                catch
                {
                }

                CustomServerJoinCancelState.FinishCancelledJoin();

                return false;
            }

            return true;
        }
    }
}