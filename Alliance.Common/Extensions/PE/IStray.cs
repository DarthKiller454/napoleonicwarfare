using System;

namespace Alliance.Common.Extensions.PE
{
    public interface IStray
    {
        bool IsStray();

        void ResetStrayDuration();
    }
}
