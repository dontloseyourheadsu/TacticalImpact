namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class PackageComponent
{
    public bool IsCarried { get; set; }
    public int CarrierEntity { get; set; } = -1;
}
