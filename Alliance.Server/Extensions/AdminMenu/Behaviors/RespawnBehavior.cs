using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static TaleWorlds.MountAndBlade.MPPerkObject;
using static TaleWorlds.MountAndBlade.MultiplayerClassDivisions;

namespace Alliance.Server.Extensions.AdminMenu.Behaviors
{
	/// <summary>
	/// MissionBehavior used to spawn/respawn players joining during a round.
	/// </summary>
	public class RespawnBehavior : MissionNetwork, IMissionBehavior
	{
		private Dictionary<MissionPeer, BasicCharacterObject> _playersPreviousCharacter;
        private Dictionary<MissionPeer, MatrixFrame> _playersLastDeathFrame;
        private float _lastSpawnCheck;
		private BasicCharacterObject _defaultCharacter;
		private List<int> _defaultPerks;
        private HashSet<MissionPeer> _pendingRespawns;

        public RespawnBehavior() : base()
		{
			_playersPreviousCharacter = new Dictionary<MissionPeer, BasicCharacterObject>(); 
			_playersLastDeathFrame = new Dictionary<MissionPeer, MatrixFrame>();
        }

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			_defaultCharacter = MBObjectManager.Instance.GetObject<BasicCharacterObject>("mp_heavy_infantry_vlandia_troop");
			_defaultPerks = new List<int>() { 0, 0 }; 
			_pendingRespawns = new HashSet<MissionPeer>();
        }

		public override void OnRemoveBehavior()
		{

			_playersPreviousCharacter.Clear();
            _playersLastDeathFrame.Clear();
            _pendingRespawns.Clear();
            base.OnRemoveBehavior();
		}
        private bool TryGetLastDeathFrame(MissionPeer peer, out MatrixFrame frame)
		{
			if (_playersLastDeathFrame.TryGetValue(peer, out frame))
			{
				return true;
			}

            frame = default;
            return false;
        }
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent?.MissionPeer != null)
            {
                var peer = affectedAgent.MissionPeer;
            _playersLastDeathFrame[peer] = affectedAgent.Frame;
            }
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        }

		/// <summary>
		/// Respawn players during a round, depending on the server configuration.
		/// AllowSpawnInRound : Allow players to join an ongoing round.
		/// FreeRespawnTimer : Grants free respawn for a limited time at the beginning of a round.
		/// </summary>
		public void RespawnPlayer(NetworkCommunicator playerSelected)
		{

			MissionPeer missionPeer = playerSelected.GetComponent<MissionPeer>();

			if (CanPlayerBeRespawned(missionPeer) && playerSelected.IsSynchronized && (missionPeer.Team == Mission.AttackerTeam || missionPeer.Team == Mission.DefenderTeam))
			{
                _pendingRespawns.Add(missionPeer);
                SpawnPlayer(playerSelected, missionPeer, GetCharacterOfPeer(missionPeer));
			}
		}

		/// <summary>
		/// Return character last used by peer if they have same culture. Otherwise return null.
		/// </summary>
		private BasicCharacterObject GetCharacterOfPeer(MissionPeer missionPeer)
		{
			BasicCharacterObject character = PlayerSpawnMenu.Instance.GetPlayerAssignment(missionPeer.GetNetworkPeer()).Character?.Character;
			if (character != null) return character;

			if (_playersPreviousCharacter.ContainsKey(missionPeer) && _playersPreviousCharacter[missionPeer].Culture == missionPeer.Culture)
			{
				return _playersPreviousCharacter[missionPeer];
			}
			else
			{
				return null;
			}
		}

		private bool CanPlayerBeRespawned(MissionPeer missionPeer)
		{
			if (missionPeer == null || missionPeer.ControlledAgent != null)
			{
				return false;
			}

			return true;
		}

		public void SpawnPlayer(NetworkCommunicator playerSelected, MissionPeer missionPeer, BasicCharacterObject basicCharacterObject)
		{
			MPOnSpawnPerkHandler perkHandler;

			List<int> selectedPerks = PlayerSpawnMenu.Instance.GetPlayerAssignment(playerSelected).Perks;
			if (selectedPerks != null)
			{
				perkHandler = GetOnSpawnPerkHandler(SpawnHelper.GetPerks(basicCharacterObject, selectedPerks));
			}
			else
			{
				perkHandler = GetOnSpawnPerkHandler(missionPeer);
			}

			BasicCultureObject _cultureTeam;
			MPHeroClass _defaultMpClassTeam;

			if (basicCharacterObject != null)
				SpawnHelper.SpawnPlayer(playerSelected, perkHandler, basicCharacterObject);
			else

			{
				if (missionPeer.Team == Mission.AttackerTeam)
				{
					_cultureTeam = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
					_defaultMpClassTeam = MultiplayerClassDivisions.GetMPHeroClasses(_cultureTeam).FirstOrDefault();

					SpawnHelper.SpawnPlayer(playerSelected, perkHandler, _defaultMpClassTeam.HeroCharacter);
				}
				else if (missionPeer.Team == Mission.DefenderTeam)
				{
					_cultureTeam = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
					_defaultMpClassTeam = MultiplayerClassDivisions.GetMPHeroClasses(_cultureTeam).FirstOrDefault();

					SpawnHelper.SpawnPlayer(playerSelected, perkHandler, _defaultMpClassTeam.HeroCharacter);

				}
			}
		}

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent?.MissionPeer != null)
            {
                var peer = agent.MissionPeer;

                _playersPreviousCharacter[peer] = agent.Character;

                if (_pendingRespawns.Contains(peer) &&
                    _playersLastDeathFrame.TryGetValue(peer, out var frame))
                {
                    agent.TeleportToPosition(frame.origin);

                    _playersLastDeathFrame.Remove(peer);
                    _pendingRespawns.Remove(peer);
                }
            }
        }
    }
}