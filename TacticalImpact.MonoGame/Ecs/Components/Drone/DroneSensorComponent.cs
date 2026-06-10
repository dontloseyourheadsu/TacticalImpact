namespace TacticalImpact.MonoGame.Ecs.Components;

public enum SensorType
{
    OpticalCamera, // High range, vulnerable to blinding light
    LidarScanner   // Short range, immune to blinding light
}

public sealed class DroneSensorComponent
{
    public SensorType ActiveSensor { get; set; } = SensorType.OpticalCamera;
    public float BlindedDurationRemaining { get; set; } = 0f;
    
    // Tools / Upgrades
    public bool HasAntiGlareFilter { get; set; } = false; // Reduces blinding duration
    public bool HasLidarBackup { get; set; } = false;     // Can switch to Lidar when blinded
    
    // Ranges
    public float OpticalRange { get; set; } = 12f;
    public float LidarRange { get; set; } = 5f;

    public float CurrentSensorRange => (ActiveSensor == SensorType.LidarScanner) ? LidarRange : OpticalRange;
    public bool IsFullyBlinded => BlindedDurationRemaining > 0f && (!HasLidarBackup || ActiveSensor == SensorType.OpticalCamera);
}
