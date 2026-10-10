using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.GameModeMenu.Models;
using Alliance.Common.Extensions.GameModeMenu.NetworkMessages.FromClient;
using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Battle;
using Alliance.Common.GameModes.BattleRoyale;
using Alliance.Common.GameModes.Captain;
using Alliance.Common.GameModes.CvC;
using Alliance.Common.GameModes.Deathmatch;
using Alliance.Common.GameModes.Groupfight;
using Alliance.Common.GameModes.Halloween;
using Alliance.Common.GameModes.Lobby;
using Alliance.Common.GameModes.PvC;
using Alliance.Common.GameModes.Siege;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.TeamDeathmatch;
using Alliance.Common.Utilities;
using Alliance.Server.Core;
using System;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.GameModeMenu.Handlers
{
	public class PollHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<GameModePollRequestMessage>(HandleGameModePollRequest);
			reg.Register<ScenarioPollRequestMessage>(HandleScenarioPollRequest);
		}
        /// <summary>
        /// Handle game mode poll request
        /// </summary>
        public bool HandleGameModePollRequest(NetworkCommunicator peer, GameModePollRequestMessage message)
        {
            try
            {
                return HandleGameModePollRequestInternal(peer, message);
            }
            catch (Exception ex)
            {
                // ex.ToString() includes the full stack trace + inner exception, unlike the native invoker's log
                Log($"HandleGameModePollRequest crashed:\n{ex}", LogLevel.Error);
                return false;
            }
        }
        private bool HandleGameModePollRequestInternal(NetworkCommunicator peer, GameModePollRequestMessage message)
        {
            Log("Step 1: checking isInLobby", LogLevel.Debug);
            bool isInLobby = MultiplayerOptions.OptionType.GameType.GetStrValue() == "Lobby";

            Log("Step 2: reading gameType from message", LogLevel.Debug);
            string gameType = message.NativeOptions[MultiplayerOptions.OptionType.GameType].ToString();

            Log("Step 3: checking permissions", LogLevel.Debug);
            bool skipPollAdmin = message.SkipPoll && peer.IsAdmin();
            bool authorizedLobbyLaunch = isInLobby && Config.Instance.AuthorizePoll && GameModeMenuConstants.AVAILABLE_GAME_MODES.Contains(gameType);

            if (skipPollAdmin || authorizedLobbyLaunch)
            {
                Log("Step 4: reading map option", LogLevel.Debug);
                string map = message.NativeOptions[MultiplayerOptions.OptionType.Map].ToString();

                Log("Step 5: checking scene existence", LogLevel.Debug);
                bool sceneExists = SceneList.Scenes != null && SceneList.Scenes.Exists(sceneInfo => sceneInfo.Name == map);

                if (!sceneExists)
                {
                    Log("Step 6: scene not found, notifying peer", LogLevel.Debug);
                    SendMessageToPeer($"The scene \"{map}\" isn't available on this server", peer);
                    return false;
                }

                Log("Step 7: reading culture options", LogLevel.Debug);
                string cultureTeam1 = message.NativeOptions[MultiplayerOptions.OptionType.CultureTeam1].ToString();
                string cultureTeam2 = message.NativeOptions[MultiplayerOptions.OptionType.CultureTeam2].ToString();

                Log("Step 8: creating game mode settings", LogLevel.Debug);
                GameModeSettings gameModeSettings = CreateSettingsFromMessage(message);
                if (gameModeSettings == null)
                {
                    Log($"HandleGameModePollRequest: unknown GameType \"{gameType}\" from {peer.UserName}", LogLevel.Error);
                    SendMessageToPeer($"Requested game mode \"{gameType}\" is not recognized by the server.", peer);
                    return false;
                }

                Log("Step 9: starting mission", LogLevel.Debug);
                GameModeStarter.Instance.StartMission(gameModeSettings);

                string log = $"Starting {gameType} on {map} ({cultureTeam1} VS {cultureTeam2})...";
                Log(log, LogLevel.Information);
                SendMessageToAll(log);

                return true;
            }
            return false;
        }

        private GameModeSettings CreateSettingsFromMessage(GameModePollRequestMessage message)
		{
			GameModeSettings gameModeSettings;

			string gameType = message.NativeOptions[MultiplayerOptions.OptionType.GameType].ToString();

			switch (gameType)
			{
				case "CaptainX": gameModeSettings = new CaptainGameModeSettings(); break;
				case "BattleX": gameModeSettings = new BattleGameModeSettings(); break;
				case "SiegeX": gameModeSettings = new SiegeGameModeSettings(); break;
				case "Scenario": gameModeSettings = new ScenarioGameModeSettings(); break;
				case "PvC": gameModeSettings = new PvCGameModeSettings(); break;
				case "CvC": gameModeSettings = new CvCGameModeSettings(); break;
				case "BattleRoyale": gameModeSettings = new BRGameModeSettings(); break;
				case "Lobby": gameModeSettings = new LobbyGameModeSettings(); break;
                case "TeamDeathmatchX": gameModeSettings = new TeamDeathmatchGameModeSettings(); break;
                case "Deathmatch": gameModeSettings = new DeathmatchGameModeSettings(); break;
                case "Groupfight": gameModeSettings = new GroupfightGameModeSettings(); break;
                case "Halloween": gameModeSettings = new HalloweenGameModeSettings(); break;
                default:
					Log("Error in PollHandler.CreateSettingsFromMessage : the requested game mode is not handled by the method!", LogLevel.Error);
					return null;
			}

			gameModeSettings.TWOptions = message.NativeOptions;
			gameModeSettings.ModOptions = message.ModOptions;

			return gameModeSettings;
		}

		public bool HandleScenarioPollRequest(NetworkCommunicator peer, ScenarioPollRequestMessage message)
		{
			// TODO : make use of PollRequest content to start a poll

			if (message.SkipPoll && peer.IsAdmin())
			{
				Scenario currentScenario = ScenarioManager.Instance.AvailableScenario.Find(scenario => scenario.Id == message.Scenario);

				if (currentScenario != null && currentScenario.Acts.Count > message.Act)
				{
					Act currentAct = currentScenario.Acts[message.Act];
					ScenarioManager.Instance.StartScenario(currentScenario, currentAct);
				}
				else
				{
					Log($"Failed to start scenario \"{currentScenario?.Name.LocalizedText}\" at act {message.Act}", LogLevel.Error);
				}

				return true;
			}
			return false;
		}
	}
}
