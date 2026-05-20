using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;

namespace Alliance.Client.Extensions.ExNativeUI.GameLobby
{
    public static class CustomServerJoinCancelState
    {
        private static bool _isJoinPending;
        private static bool _cancelRequested;

        public static void BeginJoinAttempt()
        {
            _isJoinPending = true;
            _cancelRequested = false;
        }

        public static void Tick(float dt)
        {
            if (!_isJoinPending || _cancelRequested)
                return;

            LobbyClient gameClient = NetworkMain.GameClient;

            if (gameClient == null)
            {
                Clear();
                return;
            }

            if (Mission.Current != null)
            {
                Clear();
                return;
            }

            LobbyClient.State state = gameClient.CurrentState;

            bool isJoinState =
                state == LobbyClient.State.WaitingToJoinCustomGame ||
                state == LobbyClient.State.InCustomGame;

            if (!isJoinState)
            {
                Clear();
                return;
            }

            if (Input.IsKeyPressed(InputKey.Escape))
            {
                Cancel(gameClient);
            }
        }

        private static void Cancel(LobbyClient gameClient)
        {
            _cancelRequested = true;

            gameClient.QuitFromCustomGame();
        }

        public static bool IsCancelRequested()
        {
            return _cancelRequested;
        }

        public static void FinishCancelledJoin()
        {
            Clear();
        }

        private static void Clear()
        {
            _isJoinPending = false;
            _cancelRequested = false;
        }
    }
}