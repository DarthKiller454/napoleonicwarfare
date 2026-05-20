using Alliance.Common.Core.KeyBinder;
using Alliance.Common.Core.KeyBinder.Models;
using Alliance.Common.Extensions.Artillery;
using System.Collections.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.Extensions.Artillery.Views
{
    [DefaultView]
    public class MoveableControlHotkeyView : MissionView, IUseKeyBinder, IMoveableControlInputProvider
    {
        private static string KeyCategoryId = "nwf_artillery";

        private const string MoveForwardId = "key_moveable_forward";
        private const string MoveBackwardId = "key_moveable_backward";
        private const string TurnLeftId = "key_moveable_turn_left";
        private const string TurnRightId = "key_moveable_turn_right";
        private const string MoveUpId = "key_moveable_up";
        private const string MoveDownId = "key_moveable_down";

        private static readonly Dictionary<MoveableControlAction, GameKey> _keys =
            new Dictionary<MoveableControlAction, GameKey>();

        BindedKeyCategory IUseKeyBinder.BindedKeys => new BindedKeyCategory()
        {
            CategoryId = KeyCategoryId,
            Category = "NWF - Moveable Controls",
            Keys = new List<BindedKey>()
            {
                new BindedKey()
                {
                    Id = MoveForwardId,
                    Name = "Move Forward",
                    Description = "Move the controlled object forward.",
                    DefaultInputKey = InputKey.W
                },
                new BindedKey()
                {
                    Id = MoveBackwardId,
                    Name = "Move Backward",
                    Description = "Move the controlled object backward.",
                    DefaultInputKey = InputKey.S
                },
                new BindedKey()
                {
                    Id = TurnLeftId,
                    Name = "Turn Left",
                    Description = "Turn the controlled object left.",
                    DefaultInputKey = InputKey.A
                },
                new BindedKey()
                {
                    Id = TurnRightId,
                    Name = "Turn Right",
                    Description = "Turn the controlled object right.",
                    DefaultInputKey = InputKey.D
                },
                new BindedKey()
                {
                    Id = MoveUpId,
                    Name = "Move Up",
                    Description = "Move the controlled object up.",
                    DefaultInputKey = InputKey.Space
                },
                new BindedKey()
                {
                    Id = MoveDownId,
                    Name = "Move Down",
                    Description = "Move the controlled object down.",
                    DefaultInputKey = InputKey.LeftShift
                }
            }
        };

        public override void EarlyStart()
        {
            base.EarlyStart();

            ResolveKeys();

            MoveableControlInput.RegisterProvider(this);
        }

        protected override void OnEndMission()
        {
            MoveableControlInput.UnregisterProvider(this);

            base.OnEndMission();
        }

        private static void ResolveKeys()
        {
            _keys.Clear();

            GameKeyContext category = HotKeyManager.GetCategory(KeyCategoryId);

            if (category == null || category.RegisteredGameKeys == null)
                return;

            RegisterKey(category.RegisteredGameKeys, MoveableControlAction.MoveForward, MoveForwardId);
            RegisterKey(category.RegisteredGameKeys, MoveableControlAction.MoveBackward, MoveBackwardId);
            RegisterKey(category.RegisteredGameKeys, MoveableControlAction.TurnLeft, TurnLeftId);
            RegisterKey(category.RegisteredGameKeys, MoveableControlAction.TurnRight, TurnRightId);
            RegisterKey(category.RegisteredGameKeys, MoveableControlAction.MoveUp, MoveUpId);
            RegisterKey(category.RegisteredGameKeys, MoveableControlAction.MoveDown, MoveDownId);
        }

        private static void RegisterKey(
            List<GameKey> registeredGameKeys,
            MoveableControlAction action,
            string keyId)
        {
            GameKey gameKey = registeredGameKeys.Find(gk => gk != null && gk.StringId == keyId);

            if (gameKey != null)
            {
                _keys[action] = gameKey;
            }
        }

        private static GameKey GetResolvedGameKey(MoveableControlAction action)
        {
            GameKey gameKey;

            if (!_keys.TryGetValue(action, out gameKey) || gameKey == null)
            {
                ResolveKeys();

                if (!_keys.TryGetValue(action, out gameKey))
                    return null;
            }

            return gameKey;
        }

        public bool IsPressed(MoveableControlAction action)
        {
            GameKey gameKey = GetResolvedGameKey(action);

            if (gameKey == null)
                return false;

            return IsInputKeyPressed(gameKey.KeyboardKey.InputKey)
                   || IsInputKeyPressed(gameKey.ControllerKey.InputKey);
        }

        public bool IsReleased(MoveableControlAction action)
        {
            GameKey gameKey = GetResolvedGameKey(action);

            if (gameKey == null)
                return false;

            return IsInputKeyReleased(gameKey.KeyboardKey.InputKey)
                   || IsInputKeyReleased(gameKey.ControllerKey.InputKey);
        }

        public bool IsDown(MoveableControlAction action)
        {
            GameKey gameKey = GetResolvedGameKey(action);

            if (gameKey == null)
                return false;

            return IsInputKeyDown(gameKey.KeyboardKey.InputKey)
                   || IsInputKeyDown(gameKey.ControllerKey.InputKey);
        }

        private static bool IsInputKeyPressed(InputKey inputKey)
        {
            return inputKey != InputKey.Invalid
                   && Mission.Current != null
                   && Mission.Current.InputManager != null
                   && Mission.Current.InputManager.IsKeyPressed(inputKey);
        }

        private static bool IsInputKeyReleased(InputKey inputKey)
        {
            return inputKey != InputKey.Invalid
                   && Mission.Current != null
                   && Mission.Current.InputManager != null
                   && Mission.Current.InputManager.IsKeyReleased(inputKey);
        }

        private static bool IsInputKeyDown(InputKey inputKey)
        {
            return inputKey != InputKey.Invalid
                   && Mission.Current != null
                   && Mission.Current.InputManager != null
                   && Mission.Current.InputManager.IsKeyDown(inputKey);
        }
    }
}