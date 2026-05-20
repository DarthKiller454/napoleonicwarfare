using System;
using System.Threading.Tasks;

namespace Alliance.Client.Extensions.CustomName
{
    public static class ClientNicknameManager
    {
        private static readonly object _lock = new object();

        private static Task<string> _currentLoadTask;
        private static string _loadedPlayerId;

        public static string CachedNickname { get; private set; }
        public static bool HasLoadedServerNickname { get; private set; }

        public static void SetCachedNickname(string nickname, bool markServerLoaded)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                return;

            CachedNickname = nickname;

            if (markServerLoaded)
                HasLoadedServerNickname = true;
        }

        public static Task<string> GetOrLoadNicknameAsync(string playerId, bool forceReload = false)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return Task.FromResult<string>(null);

            lock (_lock)
            {
                if (!forceReload &&
                    HasLoadedServerNickname &&
                    _loadedPlayerId == playerId &&
                    !string.IsNullOrWhiteSpace(CachedNickname))
                {
                    return Task.FromResult(CachedNickname);
                }

                if (!forceReload &&
                    _currentLoadTask != null &&
                    _loadedPlayerId == playerId)
                {
                    return _currentLoadTask;
                }

                _loadedPlayerId = playerId;
                _currentLoadTask = LoadNicknameInternalAsync(playerId);
                return _currentLoadTask;
            }
        }

        private static async Task<string> LoadNicknameInternalAsync(string playerId)
        {
            try
            {
                string nickname = await NicknameSender.GetNicknameFromServer(playerId);

                if (!string.IsNullOrWhiteSpace(nickname))
                {
                    SetCachedNickname(nickname, markServerLoaded: true);
                    return nickname;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Nickname] Failed to load nickname: " + ex);
                return null;
            }
            finally
            {
                lock (_lock)
                {
                    // If loading failed or no nickname existed, allow a later retry.
                    if (!HasLoadedServerNickname)
                        _currentLoadTask = null;
                }
            }
        }

        public static void ApplyToLobbyVM()
        {
            if (string.IsNullOrWhiteSpace(CachedNickname))
                return;

            if (NWFLobbyHomeVM.Instance != null)
                NWFLobbyHomeVM.Instance.ApplyNicknameLocally(CachedNickname);
        }
    }
}