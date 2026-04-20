namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class PackageZoneStatusComponent
{
    public bool IsInZone { get; set; }
    public int ZoneEntity { get; set; } = -1;
}