using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.PlayerServices;
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
                Harmony.Patch(reconnectTarget, postfix: new HarmonyMethod(reconnectPostfix));

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

            string playerId = __instance?.PlayerConnectionInfo?.PlayerID.ToString();
            if (string.IsNullOrWhiteSpace(playerId))
                return;

            string nickname = NicknameDatabase.GetNickname(playerId);
            if (!string.IsNullOrWhiteSpace(nickname))
            {
                Log($"[NicknamePatch] Reapplying nickname for reconnect: '{nickname}'", LogLevel.Information);

                // Apply to PlayerConnectionInfo
                __instance.PlayerConnectionInfo.Name = nickname;

                // Apply to VirtualPlayer.UserName via reflection (private setter)
                var userNameProperty = typeof(VirtualPlayer).GetProperty("UserName", BindingFlags.Instance | BindingFlags.Public);
                if (__instance.VirtualPlayer != null && userNameProperty != null)
                {
                    userNameProperty.SetValue(__instance.VirtualPlayer, nickname);
                }
                else
                {
                    Log("[NicknamePatch] Failed to find VirtualPlayer.UserName property for reflection", LogLevel.Error);
                }
            }
        }
    }
}