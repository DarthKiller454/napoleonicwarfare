using TaleWorlds.Engine;

public class nwf_NoCulling : ScriptComponentBehavior
{
    protected override void OnInit()
    {
        base.OnInit();
        ApplyRecursive(GameEntity);
    }

    protected override void OnEditorInit()
    {
        base.OnEditorInit();
        ApplyRecursive(GameEntity);
    }

    private void ApplyRecursive(WeakGameEntity entity)
    {
        // Get existing flags
        var flags = entity.EntityFlags;

        // Add NoOcclusionCulling
        flags |= EntityFlags.NoOcclusionCulling;

        // Set them back
        entity.SetEntityFlags(flags);

        // Recurse on children
        foreach (var child in entity.GetChildren())
        {
            ApplyRecursive(child);
        }
    }
}