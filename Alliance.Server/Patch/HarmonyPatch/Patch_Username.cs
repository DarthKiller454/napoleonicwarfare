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

                MethodInfo target = typeof(GameNetwork).GetMethod(
                    "AddNewPlayerOnServer",
                    BindingFlags.Static | BindingFlags.Public);

                MethodInfo prefix = typeof(Patch_Username).GetMethod(
                    nameof(Prefix_AddNewPlayerOnServer),
                    BindingFlags.Static | BindingFlags.Public);

                Harmony.Patch(target, prefix: new HarmonyMethod(prefix));

                MethodInfo reconnectTarget = typeof(NetworkCommunicator).GetMethod(
                    "UpdateConnectionInfoForReconnect",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                Harmony.Patch(
                    reconnectTarget,
                    prefix: new HarmonyMethod(typeof(Patch_Username).GetMethod(
                        nameof(Prefix_UpdateConnectionInfoForReconnect),
                        BindingFlags.Static | BindingFlags.Public)),
                    postfix: new HarmonyMethod(typeof(Patch_Username).GetMethod(
                        nameof(Postfix_UpdateConnectionInfoForReconnect),
                        BindingFlags.Static | BindingFlags.Public)));
                MethodInfo addNetworkPeerTarget = typeof(GameNetwork).GetMethod(
                    "AddNetworkPeer",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                MethodInfo addNetworkPeerPrefix = typeof(Patch_Username).GetMethod(
                    nameof(Prefix_AddNetworkPeer),
                    BindingFlags.Static | BindingFlags.Public);

                Harmony.Patch(addNetworkPeerTarget, prefix: new HarmonyMethod(addNetworkPeerPrefix));
                return true;
            }
            catch (Exception ex)
            {
                Log("Patch_AddNewPlayerOnServer: Error during patching", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
                return false;
            }
        }
        public static void Prefix_AddNetworkPeer(NetworkCommunicator networkPeer)
        {
            if (networkPeer == null || networkPeer.PlayerConnectionInfo == null)
                return;

            string desiredName = networkPeer.PlayerConnectionInfo.Name;

            if (string.IsNullOrWhiteSpace(desiredName))
                return;

            ForceSetNetworkCommunicatorName(networkPeer, desiredName);

            Log($"[NicknamePatch] AddNetworkPeer prefix forced name to '{desiredName}', current='{networkPeer.UserName}'", LogLevel.Information);
        }
        public static bool Prefix_AddNewPlayerOnServer(ref PlayerConnectionInfo playerConnectionInfo, ref bool serverPeer, ref bool isAdmin)
        {

            if (playerConnectionInfo == null)
                return true;

            string playerId = playerConnectionInfo.PlayerID.ToString();
            string nickname = NicknameDatabase.GetNickname(playerId);

            if (!string.IsNullOrWhiteSpace(nickname))
            {
                playerConnectionInfo.Name = nickname;
            }

            return true;
        }
        public static void Prefix_UpdateConnectionInfoForReconnect(
    NetworkCommunicator __instance,
    ref PlayerConnectionInfo playerConnectionInfo,
    bool isAdmin)
        {
            if (__instance == null || playerConnectionInfo == null)
                return;

            // playerConnectionInfo.Name was already corrected by AddNewPlayerOnServer prefix.
            string desiredName = playerConnectionInfo.Name;

            if (string.IsNullOrWhiteSpace(desiredName))
                return;

            ForceSetNetworkCommunicatorName(__instance, desiredName);

            Log($"[NicknamePatch] Reconnect prefix forced name to '{desiredName}', current communicator.UserName='{__instance.UserName}'", LogLevel.Information);
        }

        public static void Postfix_UpdateConnectionInfoForReconnect(
            NetworkCommunicator __instance,
            PlayerConnectionInfo playerConnectionInfo,
            bool isAdmin)
        {
            if (__instance == null || playerConnectionInfo == null)
                return;

            string desiredName = playerConnectionInfo.Name;

            if (string.IsNullOrWhiteSpace(desiredName))
                return;

            ForceSetNetworkCommunicatorName(__instance, desiredName);

            Log($"[NicknamePatch] Reconnect postfix forced name to '{desiredName}', current communicator.UserName='{__instance.UserName}'", LogLevel.Information);
        }

        private static void ForceSetNetworkCommunicatorName(NetworkCommunicator communicator, string desiredName)
        {
            if (communicator == null || string.IsNullOrWhiteSpace(desiredName))
                return;

            try
            {
                if (communicator.PlayerConnectionInfo != null)
                    communicator.PlayerConnectionInfo.Name = desiredName;

                if (communicator.VirtualPlayer != null)
                    ForceSetObjectStringMember(communicator.VirtualPlayer, "UserName", desiredName);

                // Optional fallback: in case NetworkCommunicator itself has a cached username field.
                ForceSetLikelyStringFields(communicator, communicator.UserName, desiredName);

                if (communicator.UserName != desiredName)
                {
                    Log(
                        $"[NicknamePatch] WARNING: communicator.UserName still not changed. Desired='{desiredName}', Actual='{communicator.UserName}'",
                        LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                Log("[NicknamePatch] Failed to force communicator username", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }

        private static void ForceSetObjectStringMember(object obj, string memberName, string value)
        {
            if (obj == null)
                return;

            Type type = obj.GetType();

            while (type != null)
            {
                PropertyInfo property = type.GetProperty(
                    memberName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                MethodInfo setter = property?.GetSetMethod(true);

                if (setter != null)
                {
                    setter.Invoke(obj, new object[] { value });
                    return;
                }

                FieldInfo backingField = type.GetField(
                    $"<{memberName}>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

                if (backingField != null)
                {
                    backingField.SetValue(obj, value);
                    return;
                }

                FieldInfo directField = type.GetField(
                    memberName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

                if (directField != null && directField.FieldType == typeof(string))
                {
                    directField.SetValue(obj, value);
                    return;
                }

                type = type.BaseType;
            }
        }

        private static void ForceSetLikelyStringFields(object obj, string oldValue, string newValue)
        {
            if (obj == null)
                return;

            Type type = obj.GetType();

            while (type != null)
            {
                FieldInfo[] fields = type.GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fields)
                {
                    if (field.FieldType != typeof(string))
                        continue;

                    string fieldName = field.Name.ToLowerInvariant();

                    bool likelyNameField =
                        fieldName.Contains("username") ||
                        fieldName.Contains("user_name") ||
                        fieldName.Contains("playername") ||
                        fieldName.Contains("name");

                    if (!likelyNameField)
                        continue;

                    object currentValue = field.GetValue(obj);

                    if (currentValue == null || currentValue.Equals(oldValue))
                    {
                        field.SetValue(obj, newValue);
                    }
                }

                type = type.BaseType;
            }
        }
    }
}