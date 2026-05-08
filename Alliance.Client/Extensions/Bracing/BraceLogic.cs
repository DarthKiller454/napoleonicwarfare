using Alliance.Client.Extensions.AnimationPlayer;
using Alliance.Common.Core.KeyBinder;
using Alliance.Common.Core.KeyBinder.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.Bracing.NetworkMessages.FromClient;
using System.Collections.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.Extensions.Bracing.Views
{
    [DefaultView]
    public class BraceView : MissionView, IUseKeyBinder
    {
        private static string KeyCategoryId = "bracing";
        private GameKey braceKey;

        BindedKeyCategory IUseKeyBinder.BindedKeys => new BindedKeyCategory()
        {
            CategoryId = KeyCategoryId,
            Category = "NWF - Combat",
            Keys = new List<BindedKey>()
            {
                new BindedKey()
                {
                    Id = "key_brace_toggle",
                    Name = "Brace Weapon",
                    Description = "Toggle musket bracing mode.",
                    DefaultInputKey = InputKey.B
                }
            }
        };

        public override void OnMissionScreenTick(float dt)
        {
            TickInputs();
        }
        public override void EarlyStart()
        {
            AnimationRequestEmitter.Instance.LastRequest = 0;
            braceKey = HotKeyManager.GetCategory(KeyCategoryId).RegisteredGameKeys.Find(gk => gk != null && gk.StringId == "key_brace_toggle");
        }
        private void TickInputs()
        {
                if (Input.IsKeyPressed(braceKey.ControllerKey.InputKey) || Input.IsKeyPressed(braceKey.KeyboardKey.InputKey))
                {
                Agent agent = Agent.Main;
                if (agent != null && agent.IsActive())
                    { 
                        SendBraceRequest(agent); 
                    }
                }
        }

        private void SendBraceRequest(Agent agent)
        {
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new CS_Bracing(agent.Index));
            GameNetwork.EndModuleEventAsClient();
        }
    }
}