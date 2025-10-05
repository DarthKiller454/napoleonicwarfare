using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.MissionLobbyComponent;

public class DynamicFormationAssignmentBehavior : MissionBehavior
{
    private readonly Dictionary<FormationClass, HashSet<string>> _formationAssignments = new();
    private readonly List<Agent> _pendingAssignments = new();

    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

    public DynamicFormationAssignmentBehavior()
    {
        AddTroopsToFormation(FormationClass.Infantry, new[]
        {
            "mp_napoleonic_austria_infantry_1_troop",
            "mp_napoleonic_austria_infantry_4_troop",
            "mp_napoleonic_austria_infantry_19_troop",
            "mp_napoleonic_austria_infantry_22_troop",
            "mp_napoleonic_austria_infantry_51_troop",
            "mp_napoleonic_austria_infantry_59_troop",
            "mp_napoleonic_britain_infantry_troop",
            "mp_napoleonic_britain_infantry_32nd_troop",
            "mp_napoleonic_britain_infantry_highland_troop",
            "mp_napoleonic_france_infantry_old_troop",
            "mp_napoleonic_france_infantry_troop",
            "mp_napoleonic_prussia_infantry_troop",
            "mp_napoleonic_prussia_landwehr_troop",
            "mp_napoleonic_prussia_freikorps_troop",
        });

        AddTroopsToFormation(FormationClass.Ranged, new[]
        {
            "mp_napoleonic_france_rifleman_troop",
            "mp_napoleonic_austria_rifleman_troop",
            "mp_napoleonic_britain_rifleman_troop",
            "mp_napoleonic_prussia_rifleman_troop",
        });

        AddTroopsToFormation(FormationClass.Cavalry, new[]
        {
            "mp_napoleonic_france_hussar_troop",
            "mp_napoleonic_britain_hussar_troop",
            "mp_napoleonic_austria_hussar_troop",
            "mp_napoleonic_prussia_hussar_troop",
        });

        AddTroopsToFormation(FormationClass.HorseArcher, new[]
        {
            "mp_napoleonic_prussia_dragoon_troop",
            "mp_napoleonic_france_dragoon_troop",
            "mp_napoleonic_austria_dragoon_troop",
        });

        AddTroopsToFormation(FormationClass.LightCavalry, new[]
        {
            "mp_napoleonic_britain_light_dragoon_troop",
            "mp_napoleonic_austria_chevauxleger_troop",
        });

        AddTroopsToFormation(FormationClass.HeavyCavalry, new[]
        {
            "mp_napoleonic_prussia_cuirassier_troop",
            "mp_napoleonic_france_cuirassier_troop",
            "mp_napoleonic_austria_cuirassier_troop",
            "mp_napoleonic_britain_dragoon_troop",
        });

        AddTroopsToFormation(FormationClass.HeavyInfantry, new[]
        {
            "mp_napoleonic_france_grenadier_old_troop",
            "mp_napoleonic_france_youngguard_troop",
            "mp_napoleonic_france_guard_troop",
            "mp_napoleonic_britain_guard_troop",
            "mp_napoleonic_france_grenadier_troop",
            "mp_napoleonic_austria_grenadier_troop",
            "mp_napoleonic_prussia_guard_troop",
        });

        AddTroopsToFormation(FormationClass.Skirmisher, new[]
        {
            "mp_napoleonic_prussia_light_troop",
            "mp_napoleonic_austria_grenzer_troop",
            "mp_napoleonic_france_voltigeur_troop",
            "mp_napoleonic_britain_light_troop",
            "mp_napoleonic_britain_medic_troop",
            "mp_napoleonic_france_medic_troop",
            "mp_napoleonic_prussia_medic_troop",
            "mp_napoleonic_austria_medic_troop",
            "mp_napoleonic_austria_artillery_troop",
            "mp_napoleonic_prussia_artillery_troop",
            "mp_napoleonic_france_artillery_troop",
            "mp_napoleonic_britain_artillery_troop",
        });
    }

    private void AddTroopsToFormation(FormationClass formationClass, IEnumerable<string> troopIds)
    {
        if (!_formationAssignments.TryGetValue(formationClass, out var troopSet))
        {
            troopSet = new HashSet<string>();
            _formationAssignments[formationClass] = troopSet;
        }

        foreach (var id in troopIds)
            troopSet.Add(id);
    }
    private MissionLobbyComponent _lobby;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();
        _lobby = Mission.Current?.GetMissionBehavior<MissionLobbyComponent>();
    }

    public override void OnAgentBuild(Agent agent, Banner banner)
    {
        if (!agent.IsHuman || !agent.IsAIControlled || agent.Team == null || agent.Character == null)
            return;

        _pendingAssignments.Add(agent);
    }

    public override void OnMissionTick(float dt)
    {
        if (_pendingAssignments.Count == 0)
            return;
        MissionLobbyComponent lobby = Mission.Current?.GetMissionBehavior<MissionLobbyComponent>();

        if (lobby == null || lobby.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Playing)
            return;

        for (int i = _pendingAssignments.Count - 1; i >= 0; i--)
        {
            var agent = _pendingAssignments[i];
            if (agent == null || !agent.IsActive() || agent.Team == null || agent.Character == null || agent.Formation == null)
                continue;

            if (agent.HasBeenBuilt && agent.Formation != null && agent.Team.FormationsIncludingEmpty.Count >= 8 && agent.Formation.HasBeenPositioned)
            {
                AssignAgentToFormation(agent);
                _pendingAssignments.RemoveAt(i);
            }
        }
    }

    private void AssignAgentToFormation(Agent agent)
    {
        string troopId = agent.Character?.StringId;
        if (string.IsNullOrEmpty(troopId) || agent.Team == null)
            return;

        foreach (var kvp in _formationAssignments)
        {
            if (kvp.Value.Contains(troopId))
            {
                Formation targetFormation = agent.Team.GetFormation(kvp.Key);
                if (targetFormation == null || agent.Formation == targetFormation)
                    return;

                ReassignAgentToFormation(agent, targetFormation);
                break;
            }
        }
    }

    private void ReassignAgentToFormation(Agent agent, Formation targetFormation)
    {
        if (agent == null || targetFormation == null || agent.Formation == targetFormation)
            return;

        if (!agent.HasBeenBuilt)
            return;

        agent.Formation = targetFormation;
    }
}
