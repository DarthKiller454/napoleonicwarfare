using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.CombatLogic
{
    public sealed class VoiceReplacer : MissionLogic
    {
        /*
        * 0-7 = default male voices
        * 8-12 = default female voices
        * 13-16 = horse
        * 17 = camel
        * 18-23 = animals
        * 24 = zombie
        * 25 = german prussian
        * 26 = german austrian
        * 27 = french
        * 28 = russian
        * 29 = uk english
        */
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);

            if (agent == null || !agent.IsHuman)
                return; 
            bool gender = agent.IsFemale;
            float pitch = CalculateVoicePitch(agent.Age, gender);
            
            if (agent.Character.StringId.Contains("austria"))
                agent.AgentVisuals.SetVoiceDefinitionIndex(26, pitch);
            if (agent.Character.StringId.Contains("prussia"))
                agent.AgentVisuals.SetVoiceDefinitionIndex(25, pitch);
            if (agent.Character.StringId.Contains("french") || agent.Character.StringId.Contains("france"))
                agent.AgentVisuals.SetVoiceDefinitionIndex(27, pitch);
            if (agent.Character.StringId.Contains("russia") && !agent.Character.StringId.Contains("prussia"))
                agent.AgentVisuals.SetVoiceDefinitionIndex(28, pitch);
            if (agent.Character.StringId.Contains("britain") || agent.Character.StringId.Contains("british"))
                agent.AgentVisuals.SetVoiceDefinitionIndex(29, pitch);
        }
        private static float CalculateVoicePitch(float age, bool gender)
        {
            age = MathF.Clamp(age, 1f, 100f);

            float normalizedAge = (age - 1f) / 99f;

            float basePitch = 1f - normalizedAge * 0.65f; // ~1.0 down to ~0.35

            float noise = MBRandom.RandomFloatRanged(-0.2f, 0.2f);
            basePitch += noise;

            if (gender) // female
            {
                float femalePitch = basePitch + 0.5f;
                return MathF.Clamp(femalePitch, 1.15f, 1.55f);
            }
            else // male
            {
                return MathF.Clamp(basePitch, 0.35f, 0.95f);
            }
        }
    }
}