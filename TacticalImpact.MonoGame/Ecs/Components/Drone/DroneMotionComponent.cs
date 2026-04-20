namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class DroneMotionComponent
{
    public float MoveDistance { get; set; } = 5.0f;
    public float MoveSpeed { get; set; } = 1.0f;
    public float FloatAmplitude { get; set; } = 0.5f;
    public float FloatSpeed { get; set; } = 2.0f;
    public float TimeOffset { get; set; }
}