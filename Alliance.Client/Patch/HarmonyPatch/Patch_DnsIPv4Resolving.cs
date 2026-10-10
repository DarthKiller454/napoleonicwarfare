using HarmonyLib;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
// Credits to BRE for sharing their IPv4 resolving
namespace Alliance.Client.Patch.HarmonyPatch
{
    public static class Patch_DnsGetHostEntry
    {
        private static readonly Harmony Harmony = new(SubModule.ModuleId + nameof(Patch_DnsGetHostEntry));
        private static bool _patched;

        public static bool Patch()
        {
            if (_patched)
                return false;

            _patched = true;

            var method = typeof(Dns).GetMethod(
                nameof(Dns.GetHostEntry),
                new[] { typeof(string) });

            if (method == null)
                return false;

            var postfix = typeof(Patch_DnsGetHostEntry).GetMethod(
                nameof(Postfix),
                BindingFlags.Static | BindingFlags.NonPublic);

            Harmony.Patch(method, postfix: new HarmonyMethod(postfix));

            return true;
        }

        private static void Postfix(ref IPHostEntry __result)
        {
            if (__result?.AddressList == null)
                return;

            var ipv4 = __result.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .ToArray();

            if (ipv4.Length > 0)
                __result.AddressList = ipv4;
        }
    }

    public static class Patch_DnsGetHostAddresses
    {
        private static readonly Harmony Harmony = new(SubModule.ModuleId + nameof(Patch_DnsGetHostAddresses));
        private static bool _patched;

        public static bool Patch()
        {
            if (_patched)
                return false;

            _patched = true;

            var method = typeof(Dns).GetMethod(
                nameof(Dns.GetHostAddresses),
                new[] { typeof(string) });

            if (method == null)
                return false;

            var postfix = typeof(Patch_DnsGetHostAddresses).GetMethod(
                nameof(Postfix),
                BindingFlags.Static | BindingFlags.NonPublic);

            Harmony.Patch(method, postfix: new HarmonyMethod(postfix));

            return true;
        }

        private static void Postfix(ref IPAddress[] __result)
        {
            if (__result == null)
                return;

            var ipv4 = __result
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .ToArray();

            if (ipv4.Length > 0)
                __result = ipv4;
        }
    }
}