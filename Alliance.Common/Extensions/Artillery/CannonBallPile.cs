using Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Usables;

namespace Alliance.Common.Extensions.Artillery
{
    public class CannonBallPile : SiegeMachineStonePile
    {
        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            return new TextObject("{=!}Cannonball Pile");
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            return new TextObject("{=!}Cannonball Pile").ToString();
        }
        protected override float GetDetachmentWeightAux(BattleSideEnum side)
        {
            if (GameEntity != null)
                return base.GetDetachmentWeightAux(side);
            else
                return 0;
        }
        protected override void OnInit()
        {
            EnemyRangeToStopUsing = 5f;
            base.OnInit();
        }
    }
    public class nwf_6PounderPile : StonePile
    {
        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            if (usableGameObject.GameEntity.HasTag(AmmoPickUpTag))
            {
                TextObject textObject = new TextObject("6-Pounder Shell");
                textObject.SetTextVariable("PILE_TYPE", new TextObject("6-Pounder Shell"));
                return textObject;
            }

            return TextObject.Empty;
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            if (gameEntity.HasTag(AmmoPickUpTag))
            {
                TextObject textObject = new TextObject("{=bNYm3K6b}{KEY} Pick Up");
                textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
                return textObject.ToString();
            }

            return string.Empty;
        }
        protected override float GetDetachmentWeightAux(BattleSideEnum side)
        {
            if (GameEntity != null)
                return base.GetDetachmentWeightAux(side);
            else
                return 0;
        }
        protected override void OnInit()
        {
            EnemyRangeToStopUsing = 5f;
            base.OnInit();
        }
    }
    public class nwf_12PounderPile : StonePile
    {
        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            if (usableGameObject.GameEntity.HasTag(AmmoPickUpTag))
            {
                TextObject textObject = new TextObject("12-Pounder Shell");
                textObject.SetTextVariable("PILE_TYPE", new TextObject("12-Pounder Shell"));
                return textObject;
            }

            return TextObject.Empty;
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            if (gameEntity.HasTag(AmmoPickUpTag))
            {
                TextObject textObject = new TextObject("{=bNYm3K6b}{KEY} Pick Up");
                textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
                return textObject.ToString();
            }

            return string.Empty;
        }
        protected override float GetDetachmentWeightAux(BattleSideEnum side)
        {
            if (GameEntity != null)
                return base.GetDetachmentWeightAux(side);
            else
                return 0;
        }
        protected override void OnInit()
        {
            EnemyRangeToStopUsing = 5f;
            base.OnInit();
        }
    }
    public class nwf_24PounderPile : StonePile
    {
        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            if (usableGameObject.GameEntity.HasTag(AmmoPickUpTag))
            {
                TextObject textObject = new TextObject("24-Pounder Shell");
                textObject.SetTextVariable("PILE_TYPE", new TextObject("24-Pounder Shell"));
                return textObject;
            }

            return TextObject.Empty;
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            if (gameEntity.HasTag(AmmoPickUpTag))
            {
                TextObject textObject = new TextObject("{=bNYm3K6b}{KEY} Pick Up");
                textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
                return textObject.ToString();
            }

            return string.Empty;
        }
        protected override float GetDetachmentWeightAux(BattleSideEnum side)
        {
            if (GameEntity != null)
                return base.GetDetachmentWeightAux(side);
            else
                return 0;
        }
        protected override void OnInit()
        {
            EnemyRangeToStopUsing = 5f;
            base.OnInit();
        }
    }
    public class nwf_36PounderPile : StonePile
    {

        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            if (usableGameObject.GameEntity.HasTag(AmmoPickUpTag))
            {
                TextObject textObject = new TextObject("36-Pounder Mortar Ball");
                textObject.SetTextVariable("PILE_TYPE", new TextObject("36-Pounder Mortar Ball"));
                return textObject;
            }

            return TextObject.Empty;
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            if (gameEntity.HasTag(AmmoPickUpTag))
            {
                TextObject textObject = new TextObject("{=bNYm3K6b}{KEY} Pick Up");
                textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
                return textObject.ToString();
            }

            return string.Empty;
        }
        protected override float GetDetachmentWeightAux(BattleSideEnum side)
        {
            if (GameEntity != null)
                return base.GetDetachmentWeightAux(side);
            else
                return 0;
        }
        protected override void OnInit()
        {
            EnemyRangeToStopUsing = 5f;
            base.OnInit();
        }
    }
    public class nwf_howitzerPile : StonePile
    {
        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            if (usableGameObject.GameEntity.HasTag(AmmoPickUpTag))
            {
                TextObject textObject = new TextObject("Howitzer Shell");
                textObject.SetTextVariable("PILE_TYPE", new TextObject("Howitzer Shell"));
                return textObject;
            }

            return TextObject.Empty;
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            if (gameEntity.HasTag(AmmoPickUpTag))
            {
                TextObject textObject = new TextObject("{=bNYm3K6b}{KEY} Pick Up");
                textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
                return textObject.ToString();
            }

            return string.Empty;
        }
        protected override float GetDetachmentWeightAux(BattleSideEnum side)
        {
            if (GameEntity != null)
                return base.GetDetachmentWeightAux(side);
            else
                return 0;
        }
        protected override void OnInit()
        {
            EnemyRangeToStopUsing = 5f;
            base.OnInit();
        }
    }
    public class nwf_CanisterPile : StonePile
    {
        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            if (usableGameObject.GameEntity.HasTag(AmmoPickUpTag))
            {
                TextObject textObject = new TextObject("Canister Shell");
                textObject.SetTextVariable("PILE_TYPE", new TextObject("Canister Shell"));
                return textObject;
            }

            return TextObject.Empty;
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            if (gameEntity.HasTag(AmmoPickUpTag))
            {
                TextObject textObject = new TextObject("{=bNYm3K6b}{KEY} Pick Up");
                textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
                return textObject.ToString();
            }

            return string.Empty;
        }
        protected override float GetDetachmentWeightAux(BattleSideEnum side)
        {
            if (GameEntity != null)
                return base.GetDetachmentWeightAux(side);
            else
                return 0;
        }
        protected override void OnInit()
        {
            EnemyRangeToStopUsing = 5f;
            base.OnInit();
        }
    }
   
}
