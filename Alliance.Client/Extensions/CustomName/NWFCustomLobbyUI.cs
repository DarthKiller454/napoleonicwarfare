using System;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.Library.NewsManager;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Home;

namespace Alliance.Client.Extensions.CustomName
{
    public class NWFLobbyHomeVM : MPLobbyHomeVM
    {
        public static NWFLobbyHomeVM Instance { get; private set; }

        private bool _isLoadingNickname;

        public NWFLobbyHomeVM(
            NewsManager newsManager,
            Action<MPLobbyVM.LobbyPage> onChangePageRequest)
            : base(newsManager, onChangePageRequest)
        {
            Instance = this;

            if (!string.IsNullOrWhiteSpace(ClientNicknameManager.CachedNickname))
            {
                ApplyNicknameLocally(ClientNicknameManager.CachedNickname);
            }

            _ = LoadServerNicknameAndApplyAsync();

            RefreshValues();
        }
        public void ReapplyCachedNickname()
        {
            if (string.IsNullOrWhiteSpace(ClientNicknameManager.CachedNickname))
                return;

            ApplyNicknameLocally(ClientNicknameManager.CachedNickname);
        }

        private async Task LoadServerNicknameAndApplyAsync()
        {
            if (_isLoadingNickname)
                return;

            _isLoadingNickname = true;

            try
            {
                string playerId = null;

                // The lobby PlayerData may not be ready immediately.
                for (int i = 0; i < 20; i++)
                {
                    playerId = GetLobbyPlayerId();

                    if (!string.IsNullOrWhiteSpace(playerId))
                        break;

                    await Task.Delay(250);
                }

                if (string.IsNullOrWhiteSpace(playerId))
                {
                    Console.WriteLine("[Nickname] Could not load nickname because lobby playerId was unavailable.");
                    return;
                }

                string nickname = await ClientNicknameManager.GetOrLoadNicknameAsync(playerId);

                if (string.IsNullOrWhiteSpace(nickname))
                    return;

                // Do not apply to an old VM instance.
                if (Instance != this)
                    return;

                ApplyNicknameLocally(nickname);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Nickname] Failed while applying server nickname: " + ex);
            }
            finally
            {
                _isLoadingNickname = false;
            }
        }

        private string GetLobbyPlayerId()
        {
            try
            {
                return Player?.PlayerData?.PlayerId.ToString();
            }
            catch
            {
                return null;
            }
        }

        public void ApplyNicknameLocally(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            if (Player == null)
                return;

            Player.Name = name;
            ClientNicknameManager.SetCachedNickname(name, markServerLoaded: false);
        }

        public void SetUserName(string name)
        {
            ApplyNicknameLocally(name);

            string playerId = Player?.PlayerData?.PlayerId.ToString();

            if (string.IsNullOrWhiteSpace(playerId))
                return;

            Task.Run(() =>
                NicknameSender.SendNicknameToServer(playerId, name));
        }

        private void EditUserName()
        {
            string title = "Change Username";
            string text = "Change your Name.";
            string ok = "Change";
            string cancel = "Cancel";

            InformationManager.ShowTextInquiry(
                new TextInquiryData(
                    title,
                    text,
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: ok,
                    negativeText: cancel,
                    affirmativeAction: OnFinish(),
                    negativeAction: null,
                    shouldInputBeObfuscated: false,
                    InputValidation,
                    soundEventPath: "",
                    defaultInputText: ClientNicknameManager.CachedNickname ?? Player.Name),
                false,
                false
            );
        }

        private Tuple<bool, string> InputValidation(string nickname)
        {
            if (nickname.Length >= 40)
                return Tuple.Create(false, "Please shorten your Username (max 40).");

            if (nickname.Length <= 2)
                return Tuple.Create(false, "Your Name must have more than 2 characters.");

            return Tuple.Create(true, "");
        }

        private Action<string> OnFinish()
        {
            return nickname =>
            {
                SetUserName(nickname);
            };
        }
    }
}