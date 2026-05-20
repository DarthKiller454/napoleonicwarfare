namespace Alliance.Common.Extensions.Artillery
{
    public enum MoveableControlAction
    {
        MoveForward,
        MoveBackward,
        TurnLeft,
        TurnRight,
        MoveUp,
        MoveDown,
        StopUsing
    }

    public interface IMoveableControlInputProvider
    {
        bool IsPressed(MoveableControlAction action);
        bool IsReleased(MoveableControlAction action);
        bool IsDown(MoveableControlAction action);
    }

    public static class MoveableControlInput
    {
        private static IMoveableControlInputProvider _provider;

        public static void RegisterProvider(IMoveableControlInputProvider provider)
        {
            _provider = provider;
        }

        public static void UnregisterProvider(IMoveableControlInputProvider provider)
        {
            if (ReferenceEquals(_provider, provider))
            {
                _provider = null;
            }
        }

        public static bool IsPressed(MoveableControlAction action)
        {
            return _provider != null && _provider.IsPressed(action);
        }

        public static bool IsReleased(MoveableControlAction action)
        {
            return _provider != null && _provider.IsReleased(action);
        }

        public static bool IsDown(MoveableControlAction action)
        {
            return _provider != null && _provider.IsDown(action);
        }
    }
}