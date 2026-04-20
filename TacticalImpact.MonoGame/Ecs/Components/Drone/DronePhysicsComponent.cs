using Microsoft.Xna.Framework;

namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class DronePhysicsComponent
{
    public Vector3 Velocity { get; set; } = Vector3.Zero;
    public float PositionGain { get; set; } = 2.0f;
    public float VelocityGain { get; set; } = 5.0f;
    public float LinearDamping { get; set; } = 1.8f;
}