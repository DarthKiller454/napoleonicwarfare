using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

public class nwf_NoCulling : ScriptComponentBehavior
{
    protected override void OnInit()
    {
        base.OnInit();

        GameEntity.EntityFlags |= EntityFlags.NoOcclusionCulling;

        ApplyRecursive(GameEntity);
    }
    protected override void OnEditorInit()
    {
        base.OnEditorInit();

        GameEntity.EntityFlags |= EntityFlags.NoOcclusionCulling;

        ApplyRecursive(GameEntity);
    }
    private void ApplyRecursive(GameEntity entity)
    {
        entity.EntityFlags |= EntityFlags.NoOcclusionCulling;

        foreach (var child in entity.GetChildren())
        {
            ApplyRecursive(child);
        }
    }
}