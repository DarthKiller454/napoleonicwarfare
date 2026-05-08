using HarmonyLib;
using System.Reflection;
using TaleWorlds.Library;

namespace Alliance.Client.Patch.HarmonyPatch
{
    public static class Patch_MPChatLineVM
    {
        private static readonly Harmony Harmony = new(SubModule.ModuleId + nameof(Patch_MPChatLineVM));
        private static bool _patched;

        private const float ChatVisibilityDuration = 20f;
        private const float ChatFadeOutDuration = 1f;
        private const float ForceInvisibleTime = ChatVisibilityDuration + ChatFadeOutDuration; // 21f

        public static bool Patch()
        {
            if (_patched)
                return false;
            _patched = true;

            var targetType = AccessTools.TypeByName(
                "TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.MPChatLineVM"
            );

            if (targetType == null)
                return false;

            var getActualAlpha = targetType.GetMethod("GetActualAlpha",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var forceInvisible = targetType.GetMethod("ForceInvisible",
                BindingFlags.Instance | BindingFlags.Public);

            var alphaPrefix = typeof(Patch_MPChatLineVM).GetMethod(nameof(Prefix_GetActualAlpha),
                BindingFlags.Static | BindingFlags.NonPublic);

            var invisiblePrefix = typeof(Patch_MPChatLineVM).GetMethod(nameof(Prefix_ForceInvisible),
                BindingFlags.Static | BindingFlags.NonPublic);

            if (getActualAlpha == null || forceInvisible == null || alphaPrefix == null || invisiblePrefix == null)
                return false;

            Harmony.Patch(getActualAlpha, prefix: new HarmonyMethod(alphaPrefix));
            Harmony.Patch(forceInvisible, prefix: new HarmonyMethod(invisiblePrefix));

            return true;
        }

        private static bool Prefix_GetActualAlpha(object __instance, ref float __result)
        {
            var timeField = AccessTools.Field(__instance.GetType(), "_timeSinceCreation");
            if (timeField == null)
                return true;

            float timeSinceCreation = (float)timeField.GetValue(__instance);

            if (timeSinceCreation >= ChatVisibilityDuration)
            {
                __result = MBMath.ClampFloat(
                    1f - (timeSinceCreation - ChatVisibilityDuration) / ChatFadeOutDuration,
                    0f,
                    1f
                );
            }
            else
            {
                __result = 1f;
            }

            return false;
        }

        private static bool Prefix_ForceInvisible(object __instance)
        {
            var timeField = AccessTools.Field(__instance.GetType(), "_timeSinceCreation");
            if (timeField == null)
                return true;

            timeField.SetValue(__instance, ForceInvisibleTime);

            var alphaProperty = AccessTools.Property(__instance.GetType(), "Alpha");
            if (alphaProperty == null)
                return true;

            alphaProperty.SetValue(__instance, 0f);

            return false;
        }
    }
}