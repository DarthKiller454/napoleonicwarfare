using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    public static class Patch_Username
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_Username));
        private static bool _patched;

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                MethodInfo target = typeof(GameNetwork).GetMethod("AddNewPlayerOnServer", BindingFlags.Static | BindingFlags.Public);
                MethodInfo method = typeof(Patch_Username).GetMethod(nameof(Prefix_AddNewPlayerOnServer), BindingFlags.Static | BindingFlags.Public);
                Harmony.Patch(target, new HarmonyMethod(method)); 
                MethodInfo reconnectTarget = typeof(NetworkCommunicator).GetMethod("UpdateConnectionInfoForReconnect", BindingFlags.Instance | BindingFlags.Public);
                MethodInfo reconnectPostfix = typeof(Patch_Username).GetMethod(nameof(Postfix_UpdateConnectionInfoForReconnect), BindingFlags.Static | BindingFlags.Public);
                Harmony.Patch(reconnectTarget, new HarmonyMethod(reconnectPostfix));

                return true;
            }
            catch (Exception ex)
            {
                Log("Patch_AddNewPlayerOnServer: Error during patching", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
                return false;
            }
        }

        public static bool Prefix_AddNewPlayerOnServer(ref PlayerConnectionInfo playerConnectionInfo, ref bool serverPeer, ref bool isAdmin)
        {

            string playerId = playerConnectionInfo.PlayerID.ToString();
            string nickname = NicknameDatabase.GetNickname(playerId);

            if (!string.IsNullOrWhiteSpace(nickname))
            {
                playerConnectionInfo.Name = nickname;
            }

            return true;
        }
        public static void Postfix_UpdateConnectionInfoForReconnect(NetworkCommunicator __instance)
        {
            if (__instance == null || __instance.PlayerConnectionInfo == null)
                return;

            string playerId = __instance.PlayerConnectionInfo.PlayerID.ToString();
            if (string.IsNullOrWhiteSpace(playerId))
                return;

            string nickname = NicknameDatabase.GetNickname(playerId);
            if (string.IsNullOrWhiteSpace(nickname))
                return;

            Log($"[NicknamePatch] Reapplying nickname for reconnect: '{nickname}'", LogLevel.Information);

            // --- Apply to ConnectionInfo (only cosmetic, but harmless)
            __instance.PlayerConnectionInfo.Name = nickname;

            // --- Apply properly to the actual peer (this updates everything)
            if (__instance.VirtualPlayer != null)
            {
                try
                {
                    _ = __instance.VirtualPlayer.UserName.Replace(__instance.VirtualPlayer.UserName.ToString(), nickname);
                }
                catch (Exception e)
                {
                    Log($"[NicknamePatch] Failed to send name sync message: {e}", LogLevel.Error);
                }
            }
        }
    }
}