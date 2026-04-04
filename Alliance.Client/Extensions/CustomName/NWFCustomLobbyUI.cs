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

        public NWFLobbyHomeVM(
            NewsManager newsManager,
            Action<MPLobbyVM.LobbyPage> onChangePageRequest)
            : base(newsManager, onChangePageRequest)
        {
            Instance = this;

            // Automatically apply loaded nickname
            if (!string.IsNullOrWhiteSpace(ClientNicknameManager.CachedNickname))
            {
                Player.Name = ClientNicknameManager.CachedNickname;
            }
            else
            {
                // Fallback: store the current name
                ClientNicknameManager.CachedNickname = Player.Name;
            }

            RefreshValues();
        }

        public void SetUserName(string name)
        {
            Player.Name = name;
            ClientNicknameManager.CachedNickname = name;

            Task.Run(() =>
                NicknameSender.SendNicknameToServer(Player.PlayerData.PlayerId.ToString(), name));
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