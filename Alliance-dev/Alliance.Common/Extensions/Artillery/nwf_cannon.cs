using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.Artillery
{
    public class MangonelCannon : Mangonel
    {
        // Default constructor
        public MangonelCannon()
        {

        }
        protected override float MaximumBallisticError
        {
            get
            {
                return 0.4f;
            }
        }
        public override float DirectionRestriction
        {
            get
            {
                return 100f;
            }
        }
        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            if (!gameEntity.HasTag(this.AmmoPickUpTag))
            {
                return new TextObject("12-Pounder Cannon", null).ToString();
            }
            return new TextObject("Cannonball", null).ToString();
        }
        protected override float HorizontalAimSensitivity
        {
            get
            {
                float num = 0.1f;
                foreach (CannonStandingPoint standingPoint in this._rotateStandingPoints)
                {
                    if (standingPoint.HasUser && !standingPoint.UserAgent.IsInBeingStruckAction)
                    {
                        num += 0.05f;
                    }
                }
                return num;
            }
        }
        protected override void OnInit()
        {
            base.OnInit();
            this._rotateStandingPoints = new List<CannonStandingPoint>();
        }
        // Example: Access a protected/internal property
        protected T GetProtectedProperty<T>(string propertyName)
        {
            var property = typeof(Mangonel).GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (property == null)
            {
                throw new InvalidOperationException($"Property '{propertyName}' not found.");
            }
            return (T)property.GetValue(this);
        }

        // Example: Call a protected/internal method
        protected void CallProtectedMethod(string methodName, params object[] parameters)
        {
            var method = typeof(Mangonel).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                throw new InvalidOperationException($"Method '{methodName}' not found.");
            }
            method.Invoke(this, parameters);
        }

        private List<CannonStandingPoint> _rotateStandingPoints;
    }
}