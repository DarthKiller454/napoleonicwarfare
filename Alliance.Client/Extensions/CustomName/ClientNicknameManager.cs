using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.CustomName
{
    public static class ClientNicknameManager
    {
        private static readonly HttpClient Http = new HttpClient();
        private static bool _loadedOnce = false;

        public static string CachedNickname { get; set; }

        /// <summary>
        /// Called from SubModule.OnBeforeInitialModuleScreenSetAsRoot()
        /// or from a Harmony patch on NetworkMain/GameClient initialization.
        /// </summary>
        public static async Task InitializeAsync()
        {
            if (_loadedOnce)
                return;

            _loadedOnce = true;

            try
            {
                string playerId = GetPlayerId();
                if (playerId == null)
                    return;

                var url = $"http://109.230.239.42/username_api/get_username.php?playerId={playerId}";

                string response = await Http.GetStringAsync(url);
                CachedNickname = ParseNickname(response);

                if (!string.IsNullOrWhiteSpace(CachedNickname))
                {
                    // Apply to UI if Lobby is already open
                    ApplyToLobbyVM();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Nickname] Failed to load nickname: {ex}");
            }
        }

        private static string GetPlayerId()
        {
            var id = NetworkMain.GameClient?.PlayerID;
            return id?.ToString();
        }

        private static string ParseNickname(string json)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<NicknameResponse>(json);
                return result?.nickname;
            }
            catch { return null; }
        }

        public static void ApplyToLobbyVM()
        {
            if (string.IsNullOrWhiteSpace(CachedNickname))
                return;

            if (NWFLobbyHomeVM.Instance != null)
            {
                NWFLobbyHomeVM.Instance.SetUserName(CachedNickname);
            }
        }

        private class NicknameResponse
        {
            public string nickname { get; set; }
        }
    }
}