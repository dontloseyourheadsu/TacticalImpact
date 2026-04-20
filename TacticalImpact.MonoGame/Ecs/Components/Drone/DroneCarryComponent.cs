using Microsoft.Xna.Framework;

namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class DroneCarryComponent
{
    public DroneCarryState State { get; set; } = DroneCarryState.Idle;
    public int TargetPackageEntity { get; set; } = -1;
    public int CarriedPackageEntity { get; set; } = -1;
    public Vector3 CarryTargetPosition { get; set; } = Vector3.Zero;
    public float SecureHeight { get; set; } = 2.4f;
    public float PickupTolerance { get; set; } = 0.22f;
    public float DropSnapTolerance { get; set; } = 0.12f;
}
