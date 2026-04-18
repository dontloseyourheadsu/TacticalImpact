namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class CollisionComponent
{
    public int Layer { get; set; } = 1;
    public int CollidesWithMask { get; set; } = 1;
    public float Radius { get; set; } = 0.55f;
}