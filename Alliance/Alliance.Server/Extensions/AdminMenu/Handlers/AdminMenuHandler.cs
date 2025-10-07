using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using Alliance.Server.Core.Security;
using Alliance.Server.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;
using Alliance.Server.Extensions.AdminMenu.Behaviors;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.Agent;
using Alliance.Common.Extensions.CustomScripts.Scripts;

namespace Alliance.Server.Extensions.AdminMenu.Handlers
{
	public class AdminMenuHandler : IHandlerRegister
	{
		private bool _invulnerable;

		public AdminMenuHandler()
		{
		}

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<AdminClient>(InitAdminServer);
			reg.Register<RequestNotification>(HandleNotificationRequest);
			reg.Register<SpawnHorseRequest>(HandleSpawnHorseRequest);
			reg.Register<TeleportRequest>(HandleTeleportRequest);
		}

		public bool HandleSpawnHorseRequest(NetworkCommunicator peer, SpawnHorseRequest req)
		{
			if (peer.IsAdmin())
			{
				string horseId = "mp_empire_horse_agile";
				string reinsId = "mp_imperial_riding_harness";

				ItemObject horseItem = Game.Current.ObjectManager.GetObject<ItemObject>(horseId);
				ItemObject reinsItem = Game.Current.ObjectManager.GetObject<ItemObject>(reinsId);

				// Ensure the ItemObject is a horse
				if (horseItem.IsMountable)
				{
					EquipmentElement horseEquipmentElement = new EquipmentElement(horseItem);
					EquipmentElement harnessEquipmentElement = new EquipmentElement(reinsItem);

					// Spawn the horse agent
					Agent horseAgent = Mission.Current.SpawnMonster(horseEquipmentElement, harnessEquipmentElement, new Vec3(10f, 10f, 1f), new Vec2(1, 0));

					// Make the horse move to the player
					WorldPosition target = peer.ControlledAgent.GetWorldPosition();
					horseAgent.SetScriptedPositionAndDirection(ref target, 1f, false, AIScriptedFrameFlags.None);

					return true;
				}
			}

			return false;
		}

		public bool HandleTeleportRequest(NetworkCommunicator peer, TeleportRequest req)
		{
			if (!peer.IsAdmin() || peer.ControlledAgent == null) return false;

			peer.ControlledAgent.TeleportToPosition(req.Position);
			Log($"[AdminPanel] The admin {peer.UserName} teleported to {req.Position}", LogLevel.Information);
			SendMessageToClient(peer, $"{peer.UserName} teleported to {req.Position}", AdminServerLog.ColorList.Success, true);

			return true;
		}

		public bool HandleNotificationRequest(NetworkCommunicator peer, RequestNotification notification)
		{
			if (peer.IsAdmin())
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SendNotification(notification.Text, notification.NotificationType));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}

			return false;
		}

		public bool InitAdminServer(NetworkCommunicator peer, AdminClient admin)
		{
			if (peer.IsAdmin())
			{
				if (admin.Heal)
					return HealPlayer(peer, admin);
				if (admin.HealAll)
					return HealAll(peer);
				if (admin.GodMod)
					return GodMod(peer, admin);
				if (admin.GodModAll)
					return GodModAll(peer);
				if (admin.Kill)
					return Kill(peer, admin);
				if (admin.KillAll)
					return KillAll(peer);
				if (admin.Kick)
					return Kick(peer, admin);
				if (admin.Ban)
					return Ban(peer, admin);
				if (admin.ToggleMutePlayer)
					return ToggleMutePlayer(peer, admin);
				if (admin.Respawn)
					return Respawn(peer, admin);
                if (admin.RespawnAll)
                    return RespawnAll(peer);
                if (admin.ToggleInvulnerable)
					return ToggleInvulnerable(peer, admin);
				if (admin.TeleportToPlayer)
					return TeleportToPlayer(peer, admin);
				if (admin.TeleportPlayerToYou)
					return TeleportPlayerToYou(peer, admin);
				if (admin.TeleportAllPlayerToYou)
					return TeleportAllPlayerToYou(peer);
				if (admin.SendWarningToPlayer)
					return SendWarningToPlayer(peer, admin);
			}
			if (peer.IsDev())
			{
				if (admin.SetAdmin)
					return SetAdmin(peer, admin);
			}

			return false;
		}

		public bool TeleportPlayerToYou(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			if (playerSelected == null) return false;

			teleportPlayersToYou(new List<NetworkCommunicator> { playerSelected }, peer);

			Log($"[AdminPanel] Player {playerSelected.UserName} was teleported by: {peer.UserName} ({peer.ControlledAgent.Position})", LogLevel.Information);
			SendMessageToClient(peer, $"{playerSelected.UserName} was teleported by: {peer.UserName} ({peer.ControlledAgent.Position})", AdminServerLog.ColorList.Success, true);
			return true;
		}

		public bool TeleportAllPlayerToYou(NetworkCommunicator peer)
		{
			List<NetworkCommunicator> playerSelected = GameNetwork.NetworkPeers.ToList();

			teleportPlayersToYou(playerSelected, peer);

			Log($"[AdminPanel] Everyone was teleported by: {peer.UserName} ({peer.ControlledAgent.Position})", LogLevel.Information);
			SendMessageToClient(peer, $"Everyone was teleported by: {peer.UserName} ({peer.ControlledAgent.Position})", AdminServerLog.ColorList.Success, true);
			return true;
		}

		public bool TeleportToPlayer(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Check if admin and target player both have an agent
			if (playerSelected == null || playerSelected.ControlledAgent == null || peer.ControlledAgent == null) return false;

			peer.ControlledAgent.TeleportToPosition(playerSelected.ControlledAgent.Position);

			Log($"[AdminPanel] {peer.UserName} teleported to: {playerSelected.UserName} ({peer.ControlledAgent.Position})", LogLevel.Information);
			SendMessageToClient(peer, $"{peer.UserName} teleported to: {playerSelected.UserName} ({peer.ControlledAgent.Position})", AdminServerLog.ColorList.Success, true);
			return true;
		}


		public  bool Respawn(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();
			MissionPeer missionPeer = playerSelected.GetComponent<MissionPeer>();

			if (missionPeer.Team == Mission.Current.AttackerTeam || missionPeer.Team == Mission.Current.DefenderTeam)
			{
				Mission.Current.GetMissionBehavior<RespawnBehavior>().RespawnPlayer(playerSelected);
				Log($"[AdminPanel]{playerSelected?.UserName} was revived by: {peer.UserName}", LogLevel.Information);
				SendMessageToClient(peer, $"{playerSelected?.UserName} was revived by: {peer.UserName}", AdminServerLog.ColorList.Success, true);
				return true; 
				
			}
			else
			{
				Log($"[AdminPanel] Error while reviving, player is not assigned", LogLevel.Information);
				SendMessageToClient(peer, $"[AdminPanel] Error while reviving, player is not assigned!", AdminServerLog.ColorList.Danger, true);
				return false;
			}

        }
        public bool RespawnAll(NetworkCommunicator peer)
        {
            List<NetworkCommunicator> playersSelected = GameNetwork.NetworkPeers.ToList();

            foreach (var playerSelected in playersSelected)
            {
                MissionPeer missionPeer = playerSelected.GetComponent<MissionPeer>();

                if (missionPeer.Team == Mission.Current.AttackerTeam || missionPeer.Team == Mission.Current.DefenderTeam)
                {
                    Mission.Current.GetMissionBehavior<RespawnBehavior>().RespawnPlayer(playerSelected);
                    Log($"[AdminPanel]{playerSelected?.UserName} was revived by: {peer.UserName}", LogLevel.Information);
                    SendMessageToClient(peer, $"{playerSelected?.UserName} was revived by: {peer.UserName}", AdminServerLog.ColorList.Success, true);
                    return true;
                }
                else
                {
                    Log($"[AdminPanel] Error while reviving, player is not assigned", LogLevel.Information);
                    SendMessageToClient(peer, $"[AdminPanel] Error while reviving, player is not assigned!", AdminServerLog.ColorList.Danger, true);
                    return false;
                }
            }
            SendMessageToClient(peer, $"{peer.UserName} revived all eligible players.", AdminServerLog.ColorList.Success, true);
            return true;
        }

        public bool HealPlayer(NetworkCommunicator peer, AdminClient admin)
        {
            NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

            healPlayers(new List<NetworkCommunicator> { playerSelected }, peer);

            Log($"[AdminPanel]{playerSelected?.UserName} was healed by: {peer.UserName}", LogLevel.Information);
            SendMessageToClient(peer, $"{playerSelected?.UserName} was healed by: {peer.UserName}", AdminServerLog.ColorList.Success, true);
            return true;
        }
        public bool HealAll(NetworkCommunicator peer)
		{
			List<NetworkCommunicator> playersSelected = GameNetwork.NetworkPeers.ToList();

			healPlayers(playersSelected, peer);

			Log($"[AdminPanel] {peer.UserName} healed All Players.", LogLevel.Information);
			SendMessageToClient(peer, $"{peer.UserName} healed All Players.", AdminServerLog.ColorList.Success, true);
			return true;
		}

		public bool GodMod(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			godModPlayers(new List<NetworkCommunicator> { playerSelected }, peer);

			Log($"[AdminPanel]{playerSelected.UserName} has been given God-Mode by: {peer.UserName}.", LogLevel.Information);
			SendMessageToClient(peer, $"{playerSelected.UserName} has been given God-Mode by: {peer.UserName}", AdminServerLog.ColorList.Success, true);

			return true;
		}

		public bool GodModAll(NetworkCommunicator peer)
		{
			List<NetworkCommunicator> playersSelected = GameNetwork.NetworkPeers.ToList();

			godModPlayers(playersSelected, peer);

			Log($"[AdminPanel] All Players are given God-Mode by: {peer.UserName}.", LogLevel.Information);
			SendMessageToClient(peer, $"All Players are given God-Mode by: {peer.UserName}.", AdminServerLog.ColorList.Success, true);

			return true;
		}

		public bool KillAll(NetworkCommunicator peer)
		{
			List<NetworkCommunicator> playersToKill = GameNetwork.NetworkPeers.ToList();

			killPlayers(playersToKill, peer);

            List<Agent> agents = Mission.Current.Agents.ToList();

            foreach (Agent agent in agents)
            {
                if (agent != null && agent.IsActive())
                {
                    CoreUtils.TakeDamage(agent, 2000, 2000f);
                }
            }
            Log($"[AdminPanel]{peer.UserName} Slayed everyone.", LogLevel.Information);
			SendMessageToClient(peer, $"{peer.UserName} Slayed everyone.", AdminServerLog.ColorList.Success, true);
			return true;
		}

		public bool Kill(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			if (playerSelected == null) return true;

			killPlayers(new List<NetworkCommunicator> { playerSelected }, peer);

			Log($"[AdminPanel] {playerSelected.UserName} was slain by: {peer.UserName}.", LogLevel.Information);
			SendMessageToClient(peer, $"{playerSelected.UserName} was slain by: {peer.UserName}", AdminServerLog.ColorList.Success, true);

			return true;
		}

		public bool SendWarningToPlayer(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Check si joueur existe
			if (playerSelected == null) return false;

			GameNetwork.BeginModuleEventAsServer(playerSelected);
			GameNetwork.WriteMessage(new SendNotification($"You have been warned by ({peer.UserName}) !", 0));
			GameNetwork.EndModuleEventAsServer();

			Log($"[AdminPanel] {playerSelected.UserName} has been warned by: {peer.UserName}.", LogLevel.Information);
			SendMessageToClient(peer, $"{playerSelected.UserName} has been warned by: {peer.UserName}", AdminServerLog.ColorList.Success, true);
			return true;

		}

		public bool Kick(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Check si joueur existe
			if (playerSelected == null) return false;

			Log($"[AdminPanel] {playerSelected.UserName} has been kicked by: {peer.UserName}.", LogLevel.Information);
			SendMessageToClient(peer, $"{playerSelected.UserName} has been kicked by: {peer.UserName}", AdminServerLog.ColorList.Success, true);
			MissionPeer playerToKick = playerSelected.GetComponent<MissionPeer>();
			DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer.KickPlayer(playerToKick.Peer.Id, false);
			return true;
		}

		public bool Ban(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Check si joueur existe
			if (playerSelected == null) return false;

			MissionPeer playerToKick = playerSelected.GetComponent<MissionPeer>();

			Log($"[AdminPanel] {playerSelected.UserName} has been banned by: {peer.UserName}.", LogLevel.Information);
			SendMessageToClient(peer, $"{playerSelected.UserName} has been banned by: {peer.UserName}.", AdminServerLog.ColorList.Success);

			SecurityManager.AddBan(playerSelected.VirtualPlayer);

			DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer.KickPlayer(playerToKick.Peer.Id, false);

			return true;
		}

		public bool ToggleMutePlayer(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Check if player exists
			if (playerSelected == null) return false;

			if (playerSelected.IsMuted())
			{

				GameNetwork.BeginModuleEventAsServer(playerSelected);
				GameNetwork.WriteMessage(new SendNotification($"You have been unmuted by: ({peer.UserName}.)", 0));
				GameNetwork.EndModuleEventAsServer();

				Log($"[AdminPanel] {playerSelected.UserName} is no longer being muted by: {peer.UserName}.", LogLevel.Information);
				SendMessageToClient(peer, $"{playerSelected.UserName} has been unmuted by: {peer.UserName}.", AdminServerLog.ColorList.Success, true);
				SecurityManager.RemoveMute(playerSelected.VirtualPlayer);
			}
			else
			{
				GameNetwork.BeginModuleEventAsServer(playerSelected);
				GameNetwork.WriteMessage(new SendNotification($"You have been muted by: ({peer.UserName}).", 0));
				GameNetwork.EndModuleEventAsServer();

				Log($"[AdminPanel] {playerSelected.UserName} has been muted by: {peer.UserName}.", LogLevel.Information);
				SendMessageToClient(peer, $"{playerSelected.UserName} has been muted by: {peer.UserName}", AdminServerLog.ColorList.Success, true);
				SecurityManager.AddMute(playerSelected.VirtualPlayer);
			}

			return true;
		}

		public bool SetAdmin(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

            // Check if player exists
            if (playerSelected == null) return false;

			if (playerSelected.IsAdmin())
			{
				Log($"[AdminPanel] {playerSelected.UserName} is no longer admin by: {peer.UserName}.", LogLevel.Information);
				SendMessageToClient(peer, $"{playerSelected.UserName} has been demoted as admin by: {peer.UserName}", AdminServerLog.ColorList.Success, true);
				SecurityManager.RemoveAdmin(playerSelected.VirtualPlayer);
			}
			else
			{
				Log($"[AdminPanel]{playerSelected.UserName} has been granted admin-rights by: {peer.UserName}.", LogLevel.Information);
				SendMessageToClient(peer, $"{playerSelected.UserName} has been granted admin-rights by: {peer.UserName}", AdminServerLog.ColorList.Success, true);
				SecurityManager.AddAdmin(playerSelected.VirtualPlayer);
			}

			return true;
		}

		public bool ToggleInvulnerable(NetworkCommunicator peer, AdminClient admin)
		{
			if (admin.PlayerSelected == "")
			{
				MortalityState state = _invulnerable ? MortalityState.Mortal : MortalityState.Invulnerable;
				foreach (Agent agent in Mission.Current?.AllAgents)
				{
					agent.SetMortalityState(state);
				}
				_invulnerable = !_invulnerable;
				Log($"[AdminPanel] Everyone ({Mission.Current?.AllAgents.Count}) has been made {state.ToString()} by: {peer.UserName}", LogLevel.Information);
				SendMessageToClient(peer, $"Everyone has been made {state} by: {peer.UserName}", AdminServerLog.ColorList.Success, true);
				return true;
			}

			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Si le joueur existe mais ne contrôle pas d'agent
			if (playerSelected != null && playerSelected.ControlledAgent == null) return false;
			playerSelected.ControlledAgent.ToggleInvulnerable();
			Log($"[AdminPanel] {playerSelected.UserName} has been returned {playerSelected.ControlledAgent.CurrentMortalityState} by {peer.UserName}", LogLevel.Information);
			SendMessageToClient(peer, $"{playerSelected.UserName} has been returned {playerSelected.ControlledAgent.CurrentMortalityState} by {peer.UserName}", AdminServerLog.ColorList.Success, true);
			return true;
		}

		private void SendMessageToClient(NetworkCommunicator targetPeer, string message, AdminServerLog.ColorList color, bool forAdmin = false)
		{
			if (!forAdmin)
			{
				GameNetwork.BeginModuleEventAsServer(targetPeer);
				GameNetwork.WriteMessage(new AdminServerLog(message, color));
				GameNetwork.EndModuleEventAsServer();
			}

			foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
			{
				GameNetwork.BeginModuleEventAsServer(peer);
				GameNetwork.WriteMessage(new AdminServerLog(message, color));
				GameNetwork.EndModuleEventAsServer();
			}
		}

		/// <summary>
		/// Tue les joueurs passés en paramètre (= 2000 dégats perçant à la tête)
		/// </summary>
		/// <param name="playersToKill">Liste des NetworkCommunicator à tuer</param>
		/// <param name="peer">NetworkCommunicator à l'origine de la demande, utile uniquement pour logguer en cas d'erreur</param>
		private void killPlayers(List<NetworkCommunicator> playersToKill, NetworkCommunicator peer = null)
		{
			try
			{
				foreach (NetworkCommunicator playerToKill in playersToKill)
				{
					// Check si joueur existe et contrôle un agent
					if (playerToKill == null || playerToKill.ControlledAgent == null) continue;

					CoreUtils.TakeDamage(playerToKill.ControlledAgent, 2000, 2000f);
				}
			}
			catch (Exception e)
			{
				Log($"[AdminPanel] Error at the execution of killPlayers. ({e.Message})", LogLevel.Error);
				SendMessageToClient(peer, $"[AdminPanel] Error at the execution of killPlayers.", AdminServerLog.ColorList.Danger, true);
			}
		}

		/// <summary>
		/// Passe en GodMod les joueurs passés en paramètre (= vie à 2000 et vitesse à 10)
		/// </summary>
		/// <param name="playersSelected">Liste des NetworkCommunicator à passer en GodMod</param>
		/// <param name="peer">NetworkCommunicator à l'origine de la demande, utile uniquement pour logguer en cas d'erreur</param>
		private void godModPlayers(List<NetworkCommunicator> playersSelected, NetworkCommunicator peer = null)
		{
			try
			{
				foreach (NetworkCommunicator playerSelected in playersSelected)
				{
					// Check si joueur existe et contrôle un agent
					if (playerSelected == null || playerSelected.ControlledAgent == null) continue;

					playerSelected.ControlledAgent.BaseHealthLimit = 2000;
					playerSelected.ControlledAgent.HealthLimit = 2000;
					playerSelected.ControlledAgent.Health = 2000;
					playerSelected.ControlledAgent.UpdateCustomDrivenProperties();
				}
			}
			catch (Exception e)
			{
				Log($"[AdminPanel] Error at the execution of godModPlayers. ({e.Message})", LogLevel.Error);
				SendMessageToClient(peer, $"[AdminPanel] Error at the execution of godModPlayers.", AdminServerLog.ColorList.Danger, true);
			}
		}

		/// <summary>
		/// Soigne les joueurs passés en paramètre (= passage de la vie actuelle à la vie max)
		/// </summary>
		/// <param name="playersSelected">Liste des NetworkCommunicator à tuer</param>
		/// <param name="peer">NetworkCommunicator à l'origine de la demande, utile uniquement pour logguer en cas d'erreur</param>
		private void healPlayers(List<NetworkCommunicator> playersSelected, NetworkCommunicator peer = null)
		{
			try
			{
				foreach (NetworkCommunicator playerSelected in playersSelected)
				{
					// Check si joueur existe et contrôle un agent
					if (playerSelected == null || playerSelected.ControlledAgent == null) continue;

					playerSelected.ControlledAgent.Health = playerSelected.ControlledAgent.HealthLimit;
					playerSelected.ControlledAgent.RestoreShieldHitPoints();
                }
			}
			catch (Exception e)
			{
				Log($"[AdminPanel] Error at the execution of healPlayers. ({e.Message})", LogLevel.Error);
				SendMessageToClient(peer, $"[AdminPanel] Error at the execution of healPlayers.", AdminServerLog.ColorList.Danger, true);
			}
		}

        /// <summary>
        /// Teleporte les joueurs passés en paramètre au Networkcommunicator passé en paramètre.
        /// </summary>
        /// <param name="playersToTeleport">Tous les joueurs à téléporté</param>
        /// <param name="peer">Les joueurs seront téléportés à la position de ce joueur</param>
        private void teleportPlayersToYou(List<NetworkCommunicator> playersToTeleport, NetworkCommunicator peer)
		{
			try
			{
				foreach (NetworkCommunicator playerToTeleport in playersToTeleport)
				{
					// Check if admin and target player both have an agent, also prevent admin from teleporting to himself
					if (playerToTeleport == null
						|| playerToTeleport.ControlledAgent == null
						|| peer.ControlledAgent == null
						|| peer.VirtualPlayer.Id == playerToTeleport.VirtualPlayer.Id)
					{
						continue;
					}

					playerToTeleport.ControlledAgent.TeleportToPosition(peer.ControlledAgent.Position);
				}
			}
			catch (Exception e)
			{
				Log($"[AdminPanel] Error at the execution of teleportPlayersToYou. ({e.Message})", LogLevel.Error);
				SendMessageToClient(peer, $"[AdminPanel] Error at the execution of teleportPlayersToYou.", AdminServerLog.ColorList.Danger, true);
			}
		}

	}
}
