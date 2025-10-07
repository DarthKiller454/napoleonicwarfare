using System;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.Library.NewsManager;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Home;
using TaleWorlds.PlayerServices;
using MySql.Data.MySqlClient;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.CustomName
{
    public class NWFLobbyHomeVM : MPLobbyHomeVM
    {
        public static string Username { get; private set; }
        public NWFLobbyHomeVM(NewsManager newsManager, Action<MPLobbyVM.LobbyPage> onChangePageRequest) : base(newsManager, onChangePageRequest)
        {
            NWFLobbyHomeVM.Instance = this;
            base.RefreshValues();
                
            if(Username != null)
            {
                base.Player.Name = Username;

            }
            else
            {
                base.Player.Name = base.Player.Name;

            }
        }

        public void SetUserName(string name)
        {
            base.Player.Name = name;
            Task.Run(() => NicknameSender.SendNicknameToServer(base.Player.PlayerData.PlayerId.ToString(), name));
        }

        private void EditUserName()
        {
            string titleText = "Change Username";
            string text = "Change your Name.";
            string affirmativeText = "Change";
            string negativeText = "Cancel";
            InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text, true, true, affirmativeText, negativeText, this.OnFinish(), null, false, new Func<string, Tuple<bool, string>>(this.InputValidation), "", Username ?? base.Player.Name), false, false);
        }

        private Tuple<bool, string> InputValidation(string arg)
        {
            if (arg.Length >= 40)
            {
                return Tuple.Create<bool, string>(false, "Please shorten your Username, it should be 40 letters or less .");
            }
            if (arg.Length <= 2)
            {
                return Tuple.Create<bool, string>(false, "Your Name should have more than 2 characters.");
            }
            return Tuple.Create<bool, string>(true, "");
        }

        public Action<string> OnFinish()
        {
            return delegate (string nickname)
            {
                NWFLobbyHomeVM.Instance.SetUserName(nickname);
            };
        }

        // Token: 0x04000022 RID: 34
        public static NWFLobbyHomeVM Instance;
    }
}
