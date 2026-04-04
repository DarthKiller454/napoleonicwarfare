using System;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade
{
    // Token: 0x0200029D RID: 669
    public class MissionMultiplayerFFAXClient : MissionMultiplayerGameModeBaseClient
    {
        // Token: 0x170006B8 RID: 1720
        // (get) Token: 0x060023A8 RID: 9128 RVA: 0x000848EB File Offset: 0x00082AEB
        public override bool IsGameModeUsingGold
        {
            get
            {
                return false;
            }
        }

        // Token: 0x170006B9 RID: 1721
        // (get) Token: 0x060023A9 RID: 9129 RVA: 0x000848EE File Offset: 0x00082AEE
        public override bool IsGameModeTactical
        {
            get
            {
                return false;
            }
        }

        // Token: 0x170006BA RID: 1722
        // (get) Token: 0x060023AA RID: 9130 RVA: 0x000848F1 File Offset: 0x00082AF1
        public override bool IsGameModeUsingRoundCountdown
        {
            get
            {
                return false;
            }
        }

        // Token: 0x170006BB RID: 1723
        // (get) Token: 0x060023AB RID: 9131 RVA: 0x000848F4 File Offset: 0x00082AF4
        public override MultiplayerGameType GameType
        {
            get
            {
                return MultiplayerGameType.Duel;
            }
        }

        // Token: 0x060023AC RID: 9132 RVA: 0x000848F7 File Offset: 0x00082AF7
        public override int GetGoldAmount()
        {
            return 0;
        }

        // Token: 0x060023AD RID: 9133 RVA: 0x000848FA File Offset: 0x00082AFA
        public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
        {
        }

        // Token: 0x060023AE RID: 9134 RVA: 0x000848FC File Offset: 0x00082AFC
        public override void AfterStart()
        {
            base.Mission.SetMissionMode(MissionMode.Battle, true);
        }
    }
}
