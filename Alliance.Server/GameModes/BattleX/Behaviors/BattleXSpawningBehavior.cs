using Alliance.Server.GameModes.CaptainX.Behaviors;
using NetworkMessages.FromServer;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.ObjectSystem;
using System.Reflection;
using Alliance.Common.GameModes.Battle;

namespace TaleWorlds.MountAndBlade;

public class BattleXSpawningBehavior : SpawningBehaviorBase, ICustomTroopIndexChangeHandler
{
    private const int EnforcedSpawnTimeInSeconds = 15;

    private float _spawningTimer; 
    
    private readonly HashSet<MissionPeer> _respawnSelectionPeers = new HashSet<MissionPeer>(); 

    private bool _spawningTimerTicking;

    private bool _roundInitialSpawnOver; 

    private ALMissionMultiplayerFlagDomination _flagDominationMissionController;

    private MultiplayerRoundController _roundController;

    private List<KeyValuePair<MissionPeer, Timer>> _enforcedSpawnTimers; 
    
    private static readonly FieldInfo _baseBotsCountForSidesField =
    typeof(SpawningBehaviorBase).GetField(
        "_botsCountForSides",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private bool _nativeTeamBotsSpawnedOnceThisRound;
    private bool _nativeTeamBotSpawnWasSuppressedThisRound;
    private bool _playersHaveActuallySpawnedThisRound;
    private int[] GetBaseBotsCountForSides()
    {
        return _baseBotsCountForSidesField?.GetValue(this) as int[];
    }
    private void ResetNativeTeamBotCountsToZero()
    {
        int[] counts = GetBaseBotsCountForSides();
        if (counts == null || counts.Length < 2)
            return;

        counts[(int)BattleSideEnum.Defender] = 0;
        counts[(int)BattleSideEnum.Attacker] = 0;
    }

    private bool HasAnyActualPlayerAgentSpawned()
    {
        foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
        {
            if (!networkPeer.IsSynchronized)
                continue;

            MissionPeer peer = networkPeer.GetComponent<MissionPeer>();
            if (peer == null)
                continue;

            if (peer.Team == null ||
                peer.Team.Side == BattleSideEnum.None ||
                peer.Team == base.Mission.SpectatorTeam)
            {
                continue;
            }

            if (peer.ControlledAgent != null && peer.ControlledAgent.IsActive())
                return true;
        }

        return false;
    }

    private bool ShouldDelayNativeTeamBotsUntilPlayersSpawned()
    {
        if (!NativeTeamBotsAreEnabled())
            return false;

        if (_playersHaveActuallySpawnedThisRound)
            return false;

        if (HasAnyActualPlayerAgentSpawned())
        {
            _playersHaveActuallySpawnedThisRound = true;
            return false;
        }

        return true;
    }
    private int GetDesiredNativeTeamBotsForSide(BattleSideEnum side)
    {
        if (side == BattleSideEnum.Attacker)
            return MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();

        if (side == BattleSideEnum.Defender)
            return MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();

        return 0;
    }

    private bool NativeTeamBotsAreEnabled()
    {
        return GetDesiredNativeTeamBotsForSide(BattleSideEnum.Attacker) > 0 ||
               GetDesiredNativeTeamBotsForSide(BattleSideEnum.Defender) > 0;
    }

    private bool HaveNativeTeamBotsReachedInitialTarget()
    {
        int[] counts = GetBaseBotsCountForSides();
        if (counts == null || counts.Length < 2)
            return false;

        int desiredDefender = GetDesiredNativeTeamBotsForSide(BattleSideEnum.Defender);
        int desiredAttacker = GetDesiredNativeTeamBotsForSide(BattleSideEnum.Attacker);

        return counts[(int)BattleSideEnum.Defender] >= desiredDefender &&
               counts[(int)BattleSideEnum.Attacker] >= desiredAttacker;
    }

    private void LockNativeTeamBotCountsToInitialTarget()
    {
        int[] counts = GetBaseBotsCountForSides();
        if (counts == null || counts.Length < 2)
            return;

        counts[(int)BattleSideEnum.Defender] =
            GetDesiredNativeTeamBotsForSide(BattleSideEnum.Defender);

        counts[(int)BattleSideEnum.Attacker] =
            GetDesiredNativeTeamBotsForSide(BattleSideEnum.Attacker);
    }

    public BattleXSpawningBehavior()
    {
        _enforcedSpawnTimers = new List<KeyValuePair<MissionPeer, Timer>>();
    }

    public override void Initialize(SpawnComponent spawnComponent)
    {
        base.Initialize(spawnComponent); 
        MissionLobbyEquipmentNetworkComponent.OnEquipmentRefreshed += OnRespawnPeerEquipmentUpdated;
        _flagDominationMissionController = base.Mission.GetMissionBehavior<ALMissionMultiplayerFlagDomination>();
        _roundController = base.Mission.GetMissionBehavior<MultiplayerRoundController>();
        _roundController.OnRoundStarted += RequestStartSpawnSession;
        _roundController.OnRoundEnding += base.RequestStopSpawnSession;
        _roundController.OnRoundEnding += base.SetRemainingAgentsInvulnerable;
        if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() == 0)
        {
            _roundController.EnableEquipmentUpdate();
        }

        base.OnAllAgentsFromPeerSpawnedFromVisuals += OnAllAgentsFromPeerSpawnedFromVisuals;
        base.OnPeerSpawnedFromVisuals += OnPeerSpawnedFromVisuals;
    }
    private void RemoveRespawnVisualsForPeer(MissionPeer peer)
    {
        if (peer == null)
            return;

        NetworkCommunicator networkPeer = peer.GetNetworkPeer();
        if (networkPeer == null)
            return;

        if (GameNetwork.IsServerOrRecorder)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new RemoveAgentVisualsForPeer(networkPeer));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }

        peer.HasSpawnedAgentVisuals = false;
    }
    public override void Clear()
    {
        MissionLobbyEquipmentNetworkComponent.OnEquipmentRefreshed -= OnRespawnPeerEquipmentUpdated;
        _respawnSelectionPeers.Clear();
        base.Clear();
        _roundController.OnRoundStarted -= RequestStartSpawnSession;
        _roundController.OnRoundEnding -= base.SetRemainingAgentsInvulnerable;
        _roundController.OnRoundEnding -= base.RequestStopSpawnSession;
        base.OnAllAgentsFromPeerSpawnedFromVisuals -= OnAllAgentsFromPeerSpawnedFromVisuals;
        base.OnPeerSpawnedFromVisuals -= OnPeerSpawnedFromVisuals;
    }
    public bool TryHandlePerkChange(
    NetworkCommunicator networkPeer,
    MissionPeer missionPeer,
    int perkListIndex,
    int perkIndex)
    {
        if (missionPeer == null)
            return false;

        if (!_respawnSelectionPeers.Contains(missionPeer))
            return false;

        if (missionPeer.ControlledAgent != null)
            return false;

        if (missionPeer.Team == null ||
            missionPeer.Team.Side == BattleSideEnum.None ||
            missionPeer.Team == base.Mission.SpectatorTeam)
        {
            return false;
        }

        if (!CanUpdateSpawnEquipment(missionPeer))
            return false;

        bool perkChanged = missionPeer.SelectPerk(perkListIndex, perkIndex, -1);


        if (perkChanged)
        {
            RefreshRespawnVisuals(missionPeer);
        }

        /*
         * Return true because this was a valid BattleX respawn-selection peer.
         * Even if SelectPerk returned false, we still handled the request.
         */
        return true;
    }
    public bool TryHandleTroopIndexChange(
        NetworkCommunicator networkPeer,
        MissionPeer missionPeer,
        int selectedTroopIndex)
    {
        if (missionPeer == null)
            return false;

        if (!_respawnSelectionPeers.Contains(missionPeer))
            return false;

        if (missionPeer.ControlledAgent != null)
            return false;

        if (missionPeer.Team == null ||
            missionPeer.Team.Side == BattleSideEnum.None ||
            missionPeer.Team == base.Mission.SpectatorTeam)
        {
            return false;
        }

        if (!CanUpdateSpawnEquipment(missionPeer))
            return false;

        if (missionPeer.Culture == null)
            return false;

        int troopCount = MultiplayerClassDivisions.GetMPHeroClasses(missionPeer.Culture).Count();

        if (selectedTroopIndex < 0 || selectedTroopIndex >= troopCount)
        {
            selectedTroopIndex = 0;
        }

        if (missionPeer.SelectedTroopIndex == selectedTroopIndex &&
            missionPeer.NextSelectedTroopIndex == selectedTroopIndex)
        {
            return true;
        }

        missionPeer.SelectedTroopIndex = selectedTroopIndex;
        missionPeer.NextSelectedTroopIndex = selectedTroopIndex;

        GameNetwork.BeginBroadcastModuleEvent();
        GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkPeer, missionPeer.SelectedTroopIndex));
        GameNetwork.EndBroadcastModuleEvent(
            GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers,
            networkPeer);

        RefreshRespawnVisuals(missionPeer);

        return true;
    }

    private void RefreshRespawnVisuals(MissionPeer peer)
    {
        if (peer == null)
            return;

        NetworkCommunicator networkPeer = peer.GetNetworkPeer();
        if (networkPeer == null)
            return;

        if (GameNetwork.IsServerOrRecorder)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new RemoveAgentVisualsForPeer(networkPeer));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }

        peer.HasSpawnedAgentVisuals = false;
        peer.EquipmentUpdatingExpired = false;

        if (peer.ControlledFormation != null)
        {
            peer.ControlledFormation.HasBeenPositioned = false;
            peer.ControlledFormation.SetSpawnIndex();
        }


        RespawnAgent(peer);
    }
    public void BeginRespawnSelection(MissionPeer peer)
    {
        if (peer == null)
        {
            return;
        }

        if (peer.ControlledAgent != null)
        {
            return;
        }

        if (peer.Team == null ||
            peer.Team.Side == BattleSideEnum.None ||
            peer.Team == base.Mission.SpectatorTeam)
        {
            return;
        }

        _respawnSelectionPeers.Add(peer);

        ToggleUpdatingSpawnEquipment(canUpdate: true);

        peer.EquipmentUpdatingExpired = false;
        peer.HasSpawnedAgentVisuals = false;
        peer.HasSpawnTimerExpired = true;

        RemoveRespawnVisualsForPeer(peer);
        RespawnAgent(peer);
    }
    private void OnRespawnPeerEquipmentUpdated(MissionPeer peer)
    {

        RespawnPeerEquipmentUpdated(peer);
    }

    private void RespawnPeerEquipmentUpdated(MissionPeer peer)
    {
        if (peer == null)
        {
            return;
        }

        if (!_respawnSelectionPeers.Contains(peer))
        {
            return;
        }

        if (peer.ControlledAgent != null)
        {

            _respawnSelectionPeers.Remove(peer);
            return;
        }

        if (peer.Team == null ||
            peer.Team.Side == BattleSideEnum.None ||
            peer.Team == base.Mission.SpectatorTeam)
        {
            return;
        }

        if (!CanUpdateSpawnEquipment(peer))
        {
            return;
        }

        RemoveRespawnVisualsForPeer(peer);
        peer.EquipmentUpdatingExpired = false;


        if (peer.ControlledFormation != null)
        {
            peer.ControlledFormation.HasBeenPositioned = false;
            peer.ControlledFormation.SetSpawnIndex();
        }

        RespawnAgent(peer);
    }
    
    public override void OnTick(float dt)
    {
        if (_spawningTimerTicking)
        {
            _spawningTimer += dt;
        }

        if (IsSpawningEnabled)
        {
            if (!_roundInitialSpawnOver && IsRoundInProgress())
            {
                foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                {
                    MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                    if (component?.Team != null && component.Team.Side != BattleSideEnum.None)
                    {
                        SpawnComponent.SetEarlyAgentVisualsDespawning(component);
                    }
                }

                _roundInitialSpawnOver = true;
                base.Mission.AllowAiTicking = true;
            }

            SpawnAgents();
            if (_roundInitialSpawnOver && _flagDominationMissionController.GameModeUsesSingleSpawning && _spawningTimer > (float)MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue())
            {
                IsSpawningEnabled = false;
                _spawningTimer = 0f;
                _spawningTimerTicking = false;
            }
        }
        bool delayNativeTeamBots = ShouldDelayNativeTeamBotsUntilPlayersSpawned();

        if (delayNativeTeamBots)
        {
            LockNativeTeamBotCountsToInitialTarget();
            _nativeTeamBotSpawnWasSuppressedThisRound = true;
        }
        else if (_nativeTeamBotSpawnWasSuppressedThisRound && !_nativeTeamBotsSpawnedOnceThisRound)
        {
            ResetNativeTeamBotCountsToZero();
            _nativeTeamBotSpawnWasSuppressedThisRound = false;
        }
        else if (_nativeTeamBotsSpawnedOnceThisRound && IsRoundInProgress())
        {
            LockNativeTeamBotCountsToInitialTarget();
        }

        base.OnTick(dt);

        if (!delayNativeTeamBots &&
            !_nativeTeamBotsSpawnedOnceThisRound &&
            IsRoundInProgress() &&
            NativeTeamBotsAreEnabled() &&
            HaveNativeTeamBotsReachedInitialTarget())
        {
            _nativeTeamBotsSpawnedOnceThisRound = true;
        }
    }

    public override void RequestStartSpawnSession()
    {
        if (!IsSpawningEnabled)
        {
            Mission.Current.SetBattleAgentCount(-1);
            IsSpawningEnabled = true;
            _spawningTimerTicking = true;

            _nativeTeamBotsSpawnedOnceThisRound = false;
            _nativeTeamBotSpawnWasSuppressedThisRound = false;
            _playersHaveActuallySpawnedThisRound = false;

            ResetSpawnCounts();
            ResetSpawnTimers();
        }
    }

    protected override void SpawnAgents()
    {
        BasicCultureObject @object = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
        BasicCultureObject object2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
        MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(@object, object2);
        int intValue = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue();
        foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
        {
            MissionPeer component = networkPeer.GetComponent<MissionPeer>();
            if (!networkPeer.IsSynchronized || component.Team == null || component.Team.Side == BattleSideEnum.None || (intValue == 0 && CheckIfEnforcedSpawnTimerExpiredForPeer(component)))
            {
                continue;
            }

            Team team = component.Team;
            bool num = team == base.Mission.AttackerTeam;
            _ = base.Mission.DefenderTeam;
            BasicCultureObject basicCultureObject = (num ? @object : object2);
            MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component);
            int num2 = ((_flagDominationMissionController.GetMissionType() == MultiplayerGameType.Battle) ? mPHeroClassForPeer.TroopBattleCost : mPHeroClassForPeer.TroopCost);
            if (component.ControlledAgent != null || component.HasSpawnedAgentVisuals || component.Team == null || component.Team == base.Mission.SpectatorTeam || !component.TeamInitialPerkInfoReady || !component.SpawnTimer.Check(base.Mission.CurrentTime))
            {
                continue;
            }

            int currentGoldForPeer = _flagDominationMissionController.GetCurrentGoldForPeer(component);
            if (mPHeroClassForPeer == null || (_flagDominationMissionController.UseGold() && num2 > currentGoldForPeer))
            {
                if (currentGoldForPeer >= MultiplayerClassDivisions.GetMinimumTroopCost(basicCultureObject) && component.SelectedTroopIndex != 0)
                {
                    component.SelectedTroopIndex = 0;
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkPeer, 0));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkPeer);
                }

                continue;
            }

            if (intValue == 0)
            {
                CreateEnforcedSpawnTimerForPeer(component, 15);
            }

            Formation formation = component.ControlledFormation;
            if (intValue > 0 && formation == null)
            {
                FormationClass formationIndex = component.Team.FormationsIncludingEmpty.First((Formation x) => x.PlayerOwner == null && !x.ContainsAgentVisuals && x.CountOfUnits == 0).FormationIndex;
                formation = team.GetFormation(formationIndex);
                formation.ContainsAgentVisuals = true;
                if (string.IsNullOrEmpty(formation.BannerCode))
                {
                    formation.BannerCode = component.Peer.BannerCode;
                }
            }

            MultiplayerBattleColors.MultiplayerCultureColorInfo peerColors = multiplayerBattleColors.GetPeerColors(component);
            BasicCharacterObject heroCharacter = mPHeroClassForPeer.HeroCharacter;
            AgentBuildData agentBuildData = new AgentBuildData(heroCharacter).MissionPeer(component).Team(component.Team).VisualsIndex(0)
                .Formation(formation)
                .MakeUnitStandOutOfFormationDistance(7f)
                .IsFemale(component.Peer.IsFemale)
                .BodyProperties(GetBodyProperties(component, (component.Culture == @object) ? @object : object2))
                .ClothingColor1(peerColors.ClothingColor1Uint)
                .ClothingColor2(peerColors.ClothingColor2Uint);
            MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(component);
            Equipment equipment = heroCharacter.Equipment.Clone();
            IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: true);
            if (enumerable != null)
            {
                foreach (var item in enumerable)
                {
                    equipment[item.Item1] = item.Item2;
                }
            }

            bool flag = false;
            agentBuildData.Equipment(equipment);
            if (intValue == 0 && !flag)
            {
                MatrixFrame spawnFrame = SpawnComponent.GetSpawnFrame(component.Team, equipment[EquipmentIndex.ArmorItemEndSlot].Item != null, isInitialSpawn: true);
                agentBuildData.InitialPosition(in spawnFrame.origin);
                Vec2 direction = spawnFrame.rotation.f.AsVec2.Normalized();
                agentBuildData.InitialDirection(in direction);
            }

            if (GameMode.ShouldSpawnVisualsForServer(networkPeer) && agentBuildData.AgentVisualsIndex == 0)
            {
                component.HasSpawnedAgentVisuals = true;
                component.EquipmentUpdatingExpired = false;
            }

            GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData);
            component.ControlledFormation = formation;
            if (intValue > 0)
            {
                int troopCount = MPPerkObject.GetTroopCount(mPHeroClassForPeer, intValue, onSpawnPerkHandler);
                IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipments = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: false);
                for (int i = 0; i < troopCount; i++)
                {
                    SpawnBotVisualsInPlayerFormation(component, i + 1, team, basicCultureObject, mPHeroClassForPeer.TroopCharacter.StringId, formation, flag, troopCount, alternativeEquipments);
                }
            }
        }
    }
    public void RespawnAgent(MissionPeer peer)
    {
        if (peer == null)
            return;

        NetworkCommunicator networkPeer = peer.GetNetworkPeer();
        if (networkPeer == null || !networkPeer.IsSynchronized)
            return;

        MissionPeer component = peer;

        if (component.Team == null ||
            component.Team.Side == BattleSideEnum.None ||
            component.Team == base.Mission.SpectatorTeam)
        {
            return;
        }

        if (component.ControlledAgent != null ||
            component.HasSpawnedAgentVisuals ||
            !component.TeamInitialPerkInfoReady)
        {
            return;
        }

        BasicCultureObject cultureTeam1 =
            MBObjectManager.Instance.GetObject<BasicCultureObject>(
                MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());

        BasicCultureObject cultureTeam2 =
            MBObjectManager.Instance.GetObject<BasicCultureObject>(
                MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());

        MultiplayerBattleColors multiplayerBattleColors =
            MultiplayerBattleColors.CreateWith(cultureTeam1, cultureTeam2);

        int intValue = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue();

        Team team = component.Team;
        bool isAttacker = team == base.Mission.AttackerTeam;

        BasicCultureObject basicCultureObject = isAttacker ? cultureTeam1 : cultureTeam2;

        MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer =
            MultiplayerClassDivisions.GetMPHeroClassForPeer(component);

        int currentGoldForPeer =
            _flagDominationMissionController.GetCurrentGoldForPeer(component);

        if (mPHeroClassForPeer == null)
        {
            if (currentGoldForPeer >= MultiplayerClassDivisions.GetMinimumTroopCost(basicCultureObject) &&
                component.SelectedTroopIndex != 0)
            {
                component.SelectedTroopIndex = 0;

                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkPeer, 0));
                GameNetwork.EndBroadcastModuleEvent(
                    GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers,
                    networkPeer);
            }

            mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component);

            if (mPHeroClassForPeer == null)
                return;
        }

        int troopCost =
            _flagDominationMissionController.GetMissionType() == MultiplayerGameType.Battle
                ? mPHeroClassForPeer.TroopBattleCost
                : mPHeroClassForPeer.TroopCost;

        /*
         * For revive spawning, decide whether you want to ignore gold.
         * If revive should still require gold, keep this block.
         * If revive should be free, remove this block.
         */
        if (_flagDominationMissionController.UseGold() && troopCost > currentGoldForPeer)
        {
            if (currentGoldForPeer >= MultiplayerClassDivisions.GetMinimumTroopCost(basicCultureObject) &&
                component.SelectedTroopIndex != 0)
            {
                component.SelectedTroopIndex = 0;

                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkPeer, 0));
                GameNetwork.EndBroadcastModuleEvent(
                    GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers,
                    networkPeer);
            }

            return;
        }

        Formation formation = component.ControlledFormation;

        if (intValue > 0 && formation == null)
        {
            FormationClass formationIndex =
                component.Team.FormationsIncludingEmpty
                    .First(x =>
                        x.PlayerOwner == null &&
                        !x.ContainsAgentVisuals &&
                        x.CountOfUnits == 0)
                    .FormationIndex;

            formation = team.GetFormation(formationIndex);
            formation.ContainsAgentVisuals = true;

            if (string.IsNullOrEmpty(formation.BannerCode))
            {
                formation.BannerCode = component.Peer.BannerCode;
            }
        }

        MultiplayerBattleColors.MultiplayerCultureColorInfo peerColors =
            multiplayerBattleColors.GetPeerColors(component);

        BasicCharacterObject heroCharacter = mPHeroClassForPeer.HeroCharacter;

        AgentBuildData agentBuildData =
            new AgentBuildData(heroCharacter)
                .MissionPeer(component)
                .Team(component.Team)
                .VisualsIndex(0)
                .Formation(formation)
                .MakeUnitStandOutOfFormationDistance(7f)
                .IsFemale(component.Peer.IsFemale)
                .BodyProperties(GetBodyProperties(
                    component,
                    component.Culture == cultureTeam1 ? cultureTeam1 : cultureTeam2))
                .ClothingColor1(peerColors.ClothingColor1Uint)
                .ClothingColor2(peerColors.ClothingColor2Uint);

        MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler =
            MPPerkObject.GetOnSpawnPerkHandler(component);

        Equipment equipment = heroCharacter.Equipment.Clone();

        IEnumerable<(EquipmentIndex, EquipmentElement)> alternativePlayerEquipments =
            onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: true);

        if (alternativePlayerEquipments != null)
        {
            foreach (var item in alternativePlayerEquipments)
            {
                equipment[item.Item1] = item.Item2;
            }
        }

        bool flag = false;

        agentBuildData.Equipment(equipment);

        if (intValue == 0 && !flag)
        {
            MatrixFrame spawnFrame =
                SpawnComponent.GetSpawnFrame(
                    component.Team,
                    equipment[EquipmentIndex.ArmorItemEndSlot].Item != null,
                    isInitialSpawn: true);

            agentBuildData.InitialPosition(in spawnFrame.origin);

            Vec2 direction = spawnFrame.rotation.f.AsVec2.Normalized();
            agentBuildData.InitialDirection(in direction);
        }

        if (GameMode.ShouldSpawnVisualsForServer(networkPeer) &&
            agentBuildData.AgentVisualsIndex == 0)
        {
            component.HasSpawnedAgentVisuals = true;
            component.EquipmentUpdatingExpired = false;
        }

        GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData);

        component.ControlledFormation = formation;

        if (intValue > 0)
        {
            int troopCount =
                MPPerkObject.GetTroopCount(
                    mPHeroClassForPeer,
                    intValue,
                    onSpawnPerkHandler);

            IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeBotEquipments =
                onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: false);

            for (int i = 0; i < troopCount; i++)
            {
                SpawnBotVisualsInPlayerFormation(
                    component,
                    i + 1,
                    team,
                    basicCultureObject,
                    mPHeroClassForPeer.TroopCharacter.StringId,
                    formation,
                    flag,
                    troopCount,
                    alternativeBotEquipments);
            }
        }
    }
    private new void OnPeerSpawnedFromVisuals(MissionPeer peer)
    {
        if (peer.ControlledFormation != null)
        {
            peer.ControlledAgent.Team.AssignPlayerAsSergeantOfFormation(peer, peer.ControlledFormation.FormationIndex);
        }
    }

    private new void OnAllAgentsFromPeerSpawnedFromVisuals(MissionPeer peer)
    {
        _playersHaveActuallySpawnedThisRound = true;
        if (peer.ControlledFormation != null)
        {
            peer.ControlledFormation.OnFormationDispersed();
            peer.ControlledFormation.SetMovementOrder(MovementOrder.MovementOrderFollow(peer.ControlledAgent));
            NetworkCommunicator networkPeer = peer.GetNetworkPeer();
            if (peer.BotsUnderControlAlive != 0 || peer.BotsUnderControlTotal != 0)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new BotsControlledChange(networkPeer, peer.BotsUnderControlAlive, peer.BotsUnderControlTotal));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                base.Mission.GetMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>().OnBotsControlledChanged(peer, peer.BotsUnderControlAlive, peer.BotsUnderControlTotal);
            }

            if (peer.Team == base.Mission.AttackerTeam)
            {
                base.Mission.NumOfFormationsSpawnedTeamOne++;
            }
            else
            {
                base.Mission.NumOfFormationsSpawnedTeamTwo++;
            }

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SetSpawnedFormationCount(base.Mission.NumOfFormationsSpawnedTeamOne, base.Mission.NumOfFormationsSpawnedTeamTwo));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }

        if (_flagDominationMissionController.UseGold())
        {
            bool flag = peer.Team == base.Mission.AttackerTeam;
            _ = base.Mission.DefenderTeam;
            MultiplayerClassDivisions.MPHeroClass mPHeroClass = MultiplayerClassDivisions.GetMPHeroClasses(MBObjectManager.Instance.GetObject<BasicCultureObject>(flag ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue())).ElementAt(peer.SelectedTroopIndex);
            int num = ((_flagDominationMissionController.GetMissionType() == MultiplayerGameType.Battle) ? mPHeroClass.TroopBattleCost : mPHeroClass.TroopCost);
            _flagDominationMissionController.ChangeCurrentGoldForPeer(peer, _flagDominationMissionController.GetCurrentGoldForPeer(peer) - num);
        }
        _respawnSelectionPeers.Remove(peer);
    }

    private void BotFormationSpawned(Team team)
    {
        if (team == base.Mission.AttackerTeam)
        {
            base.Mission.NumOfFormationsSpawnedTeamOne++;
        }
        else if (team == base.Mission.DefenderTeam)
        {
            base.Mission.NumOfFormationsSpawnedTeamTwo++;
        }
    }

    private void AllBotFormationsSpawned()
    {
        if (base.Mission.NumOfFormationsSpawnedTeamOne != 0 || base.Mission.NumOfFormationsSpawnedTeamTwo != 0)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SetSpawnedFormationCount(base.Mission.NumOfFormationsSpawnedTeamOne, base.Mission.NumOfFormationsSpawnedTeamTwo));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }
    }

    public override bool AllowEarlyAgentVisualsDespawning(MissionPeer lobbyPeer)
    {
        if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() == 0)
        {
            if (!_roundController.IsRoundInProgress)
            {
                return false;
            }

            if (!lobbyPeer.HasSpawnTimerExpired && lobbyPeer.SpawnTimer.Check(Mission.Current.CurrentTime))
            {
                lobbyPeer.HasSpawnTimerExpired = true;
            }

            return lobbyPeer.HasSpawnTimerExpired;
        }

        return false;
    }

    protected override bool IsRoundInProgress()
    {
        return _roundController.IsRoundInProgress;
    }

    private void CreateEnforcedSpawnTimerForPeer(MissionPeer peer, int durationInSeconds)
    {
        if (!_enforcedSpawnTimers.Any((KeyValuePair<MissionPeer, Timer> pair) => pair.Key == peer))
        {
            _enforcedSpawnTimers.Add(new KeyValuePair<MissionPeer, Timer>(peer, new Timer(base.Mission.CurrentTime, durationInSeconds)));
            Debug.Print("EST for " + peer.Name + " set to " + durationInSeconds + " seconds.", 0, Debug.DebugColor.Yellow, 64uL);
        }
    }

    private bool CheckIfEnforcedSpawnTimerExpiredForPeer(MissionPeer peer)
    {
        KeyValuePair<MissionPeer, Timer> keyValuePair = _enforcedSpawnTimers.FirstOrDefault((KeyValuePair<MissionPeer, Timer> pr) => pr.Key == peer);
        if (keyValuePair.Key == null)
        {
            return false;
        }

        if (peer.ControlledAgent != null)
        {
            _enforcedSpawnTimers.RemoveAll((KeyValuePair<MissionPeer, Timer> p) => p.Key == peer);
            Debug.Print("EST for " + peer.Name + " is no longer valid (spawned already).", 0, Debug.DebugColor.Yellow, 64uL);
            return false;
        }

        Timer value = keyValuePair.Value;
        if (peer.HasSpawnedAgentVisuals && value.Check(Mission.Current.CurrentTime))
        {
            SpawnComponent.SetEarlyAgentVisualsDespawning(peer);
            _enforcedSpawnTimers.RemoveAll((KeyValuePair<MissionPeer, Timer> p) => p.Key == peer);
            Debug.Print("EST for " + peer.Name + " has expired.", 0, Debug.DebugColor.Yellow, 64uL);
            return true;
        }

        return false;
    }

    public override void OnClearScene()
    {
        base.OnClearScene();

        _enforcedSpawnTimers.Clear();
        _roundInitialSpawnOver = false;

        _nativeTeamBotsSpawnedOnceThisRound = false;
        _nativeTeamBotSpawnWasSuppressedThisRound = false;
        _playersHaveActuallySpawnedThisRound = false;
    }

    protected void SpawnBotInBotFormation(int visualsIndex, Team agentTeam, BasicCultureObject cultureLimit, BasicCharacterObject character, Formation formation)
    {
        BasicCultureObject @object = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
        BasicCultureObject object2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
        MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(@object, object2);
        MultiplayerBattleColors.MultiplayerCultureColorInfo multiplayerCultureColorInfo = ((cultureLimit == @object) ? multiplayerBattleColors.AttackerColors : multiplayerBattleColors.DefenderColors);
        AgentBuildData agentBuildData = new AgentBuildData(character).Team(agentTeam).TroopOrigin(new BasicBattleAgentOrigin(character)).VisualsIndex(visualsIndex)
            .EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(character, visualsIndex))
            .Formation(formation)
            .IsFemale(character.IsFemale)
            .ClothingColor1(multiplayerCultureColorInfo.ClothingColor1Uint)
            .ClothingColor2(multiplayerCultureColorInfo.ClothingColor2Uint);
        agentBuildData.Equipment(Equipment.GetRandomEquipmentElements(character, !GameNetwork.IsMultiplayer, Equipment.EquipmentType.Battle, agentBuildData.AgentEquipmentSeed));
        agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, character.GetBodyPropertiesMin(), character.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, character.BodyPropertyRange.HairTags, character.BodyPropertyRange.BeardTags, character.BodyPropertyRange.TattooTags));
        base.Mission.SpawnAgent(agentBuildData).SetAlarmState(Agent.AIStateFlag.Alarmed);
    }

    protected void SpawnBotVisualsInPlayerFormation(MissionPeer missionPeer, int visualsIndex, Team agentTeam, BasicCultureObject cultureLimit, string troopName, Formation formation, bool updateExistingAgentVisuals, int totalCount, IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipments)
    {
        BasicCharacterObject @object = MBObjectManager.Instance.GetObject<BasicCharacterObject>(troopName);
        BasicCultureObject object2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
        BasicCultureObject object3 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
        MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(object2, object3);
        MultiplayerBattleColors.MultiplayerCultureColorInfo multiplayerCultureColorInfo = ((cultureLimit == object2) ? multiplayerBattleColors.AttackerColors : multiplayerBattleColors.DefenderColors);
        AgentBuildData agentBuildData = new AgentBuildData(@object).Team(agentTeam).OwningMissionPeer(missionPeer).VisualsIndex(visualsIndex)
            .TroopOrigin(new BasicBattleAgentOrigin(@object))
            .EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(@object, visualsIndex))
            .Formation(formation)
            .IsFemale(@object.IsFemale)
            .ClothingColor1(multiplayerCultureColorInfo.ClothingColor1Uint)
            .ClothingColor2(multiplayerCultureColorInfo.ClothingColor2Uint);
        Equipment randomEquipmentElements = Equipment.GetRandomEquipmentElements(@object, !GameNetwork.IsMultiplayer, Equipment.EquipmentType.Battle, MBRandom.RandomInt());
        if (alternativeEquipments != null)
        {
            foreach (var alternativeEquipment in alternativeEquipments)
            {
                randomEquipmentElements[alternativeEquipment.Item1] = alternativeEquipment.Item2;
            }
        }

        agentBuildData.Equipment(randomEquipmentElements);
        agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, @object.GetBodyPropertiesMin(), @object.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, @object.BodyPropertyRange.HairTags, @object.BodyPropertyRange.BeardTags, @object.BodyPropertyRange.TattooTags));
        NetworkCommunicator networkPeer = missionPeer.GetNetworkPeer();
        if (GameMode.ShouldSpawnVisualsForServer(networkPeer) && agentBuildData.AgentVisualsIndex == 0)
        {
            missionPeer.HasSpawnedAgentVisuals = true;
            missionPeer.EquipmentUpdatingExpired = false;
        }

        GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData, totalCount, useCosmetics: false);
    }
}