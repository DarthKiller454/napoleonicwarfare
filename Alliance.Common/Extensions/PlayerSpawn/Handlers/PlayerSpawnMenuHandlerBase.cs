using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using System.Collections.Generic;
using static Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.PlayerSpawnMenuMsg;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Handlers
{
	public abstract class PlayerSpawnMenuHandlerBase
	{
		protected class SyncContext
		{
			public PlayerSpawnMenu Menu = new PlayerSpawnMenu();
			public int CurrentSyncId;
			public bool InProgress = false;
			public int AddTeamCount = 0;
			public int AddFormationCount = 0;
			public int AddCharacterCount = 0;
			public int ReceivedMessageCount = 0;
			public int? ExpectedMessageCount = null;
		}

		protected SyncContext _syncContext = new();
		protected List<IPlayerSpawnMenuMessage> _bufferedMessages = new();

		protected virtual void HandlePlayerSpawnMenuOperation(IPlayerSpawnMenuMessage message)
		{
			if (message.SyncId != _syncContext.CurrentSyncId && message.Operation != PlayerSpawnMenuOperation.BeginMenuSync)
			{
				_bufferedMessages.Add(message);
				return;
			}

			if (!_syncContext.InProgress && message.Operation != PlayerSpawnMenuOperation.BeginMenuSync)
			{
				_bufferedMessages.Add(message);
				return;
			}

			if (message.Operation != PlayerSpawnMenuOperation.BeginMenuSync && message.Operation != PlayerSpawnMenuOperation.EndMenuSync)
			{
				_syncContext.ReceivedMessageCount++;
			}

			switch (message.Operation)
			{
				case PlayerSpawnMenuOperation.BeginMenuSync:
					BeginMenuSyncHandler(message);
					break;

				case PlayerSpawnMenuOperation.EndMenuSync:
					EndMenuSyncHandler(message);
					break;

				case PlayerSpawnMenuOperation.AddTeam:
					AddTeamHandler(message);
					break;

				case PlayerSpawnMenuOperation.AddFormation:
					AddFormationHandler(message);
					break;

				case PlayerSpawnMenuOperation.AddCharacter:
					AddCharacterHandler(message);
					break;

				case PlayerSpawnMenuOperation.RemoveTeam:
					RemoveTeamHandler(message);
					break;

				case PlayerSpawnMenuOperation.RemoveFormation:
					RemoveFormationHandler(message);
					break;

				case PlayerSpawnMenuOperation.RemoveCharacter:
					RemoveCharacterHandler(message);
					break;

				default:
					break;
			}
		}

		protected virtual void BeginMenuSyncHandler(IPlayerSpawnMenuMessage message)
		{
			_syncContext = new SyncContext
			{
				InProgress = true,
				CurrentSyncId = message.SyncId
			};

			foreach (IPlayerSpawnMenuMessage buffered in _bufferedMessages)
			{
				// Re-process buffered messages with the current SyncId
				if (buffered.SyncId == _syncContext.CurrentSyncId)
				{
					HandlePlayerSpawnMenuOperation(buffered);
				}
			}
			_bufferedMessages.Clear();
		}

		protected virtual void EndMenuSyncHandler(IPlayerSpawnMenuMessage message)
		{
			if (!_syncContext.InProgress)
			{
				return;
			}

			_syncContext.ExpectedMessageCount = message.TotalMessageCount;
			if (_syncContext.ExpectedMessageCount.HasValue && _syncContext.ReceivedMessageCount != _syncContext.ExpectedMessageCount.Value)
			{
				return;
			}

			if (_syncContext.Menu.Teams.Count == 0)
			{
			}

			PlayerSpawnMenu.Instance = _syncContext.Menu;

			_syncContext = new SyncContext();
		}

		protected virtual void AddTeamHandler(IPlayerSpawnMenuMessage message)
		{
			_syncContext.Menu?.Teams.Add(message.PlayerTeam);
		}

		protected virtual void AddFormationHandler(IPlayerSpawnMenuMessage message)
		{
			PlayerTeam team = _syncContext.Menu?.Teams.Find(t => t.Index == message.TeamIndex);
			if (team != null)
			{
				team.Formations.Add(message.PlayerFormation);
			}
		}

		protected virtual void AddCharacterHandler(IPlayerSpawnMenuMessage message)
		{
			if (message.AvailableCharacter == null)
			{
				return;
			}
			PlayerTeam team = _syncContext.Menu.Teams.Find(t => t.Index == message.TeamIndex);
			if (team != null)
			{
				PlayerFormation formation = team.Formations.Find(f => f.Index == message.FormationIndex);
				if (formation != null)
				{
					formation.AvailableCharacters.Add(message.AvailableCharacter);
				}
			}
		}

		protected virtual void RemoveTeamHandler(IPlayerSpawnMenuMessage message)
		{
			if (message.PlayerTeam == null)
			{
				return;
			}
			PlayerSpawnMenu.Instance.RemoveTeam(message.PlayerTeam);
		}

		protected virtual void RemoveFormationHandler(IPlayerSpawnMenuMessage message)
		{
			if (message.PlayerFormation == null)
			{
				return;
			}
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			team.RemoveFormation(message.PlayerFormation);
		}

		protected virtual void RemoveCharacterHandler(IPlayerSpawnMenuMessage message)
		{
			if (message.AvailableCharacter == null)
			{
				return;
			}
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			if (team != null)
			{
				PlayerFormation formation = team.Formations.Find(f => f.Index == message.FormationIndex);
				if (formation != null)
				{
					formation.AvailableCharacters.Remove(message.AvailableCharacter);
				}
			}
		}
	}
}
