using System;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade
{
    // Token: 0x0200029D RID: 669
    public class MissionMultiplayerFFAXClient : MissionMultiplayerGameModeBaseClient
    {
        // Token: 0x170006B8 RID: 1720
        // (get) Token: 0x060023A4 RID: 9124 RVA: 0x0008492F File Offset: 0x00082B2F
        public override bool IsGameModeUsingGold
        {
            get
            {
                return false;
            }
        }

        // Token: 0x170006B9 RID: 1721
        // (get) Token: 0x060023A5 RID: 9125 RVA: 0x00084932 File Offset: 0x00082B32
        public override bool IsGameModeTactical
        {
            get
            {
                return false;
            }
        }

        // Token: 0x170006BA RID: 1722
        // (get) Token: 0x060023A6 RID: 9126 RVA: 0x00084935 File Offset: 0x00082B35
        public override bool IsGameModeUsingRoundCountdown
        {
            get
            {
                return false;
            }
        }

        // Token: 0x170006BB RID: 1723
        // (get) Token: 0x060023A7 RID: 9127 RVA: 0x00084938 File Offset: 0x00082B38
        public override MultiplayerGameType GameType
        {
            get
            {
                return MultiplayerGameType.Duel;
            }
        }

        // Token: 0x060023A8 RID: 9128 RVA: 0x0008493B File Offset: 0x00082B3B
        public override int GetGoldAmount()
        {
            return 0;
        }

        // Token: 0x060023A9 RID: 9129 RVA: 0x0008493E File Offset: 0x00082B3E
        public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
        {
        }

        // Token: 0x060023AA RID: 9130 RVA: 0x00084940 File Offset: 0x00082B40
        public override void AfterStart()
        {
            base.Mission.SetMissionMode(MissionMode.Battle, true);
        }
    }
}
