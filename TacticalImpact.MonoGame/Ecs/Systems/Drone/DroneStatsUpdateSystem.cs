using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class DroneStatsUpdateSystem : ISystem
{
    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        // 1. Process statistics and sensor cooldowns for all active drones
        foreach (var entity in world.Query<DroneStatsComponent, DroneSensorComponent>())
        {
            var stats = world.GetComponent<DroneStatsComponent>(entity);
            var sensor = world.GetComponent<DroneSensorComponent>(entity);

            // Blinding duration recovery
            if (sensor.BlindedDurationRemaining > 0f)
            {
                sensor.BlindedDurationRemaining = MathF.Max(0f, sensor.BlindedDurationRemaining - deltaTimeSeconds);
                
                // Switch to Lidar if we have it and are blinded
                if (sensor.BlindedDurationRemaining > 0f && sensor.HasLidarBackup)
                {
                    sensor.ActiveSensor = SensorType.LidarScanner;
                }
                else if (sensor.BlindedDurationRemaining <= 0f)
                {
                    sensor.ActiveSensor = SensorType.OpticalCamera;
                }
            }
            else
            {
                sensor.ActiveSensor = SensorType.OpticalCamera;
            }

            // Shield Recharge Delay Cooldown
            if (stats.ShieldCooldown > 0f)
            {
                stats.ShieldCooldown = MathF.Max(0f, stats.ShieldCooldown - deltaTimeSeconds);
            }
            else if (stats.CurrentShield < stats.MaxShield)
            {
                stats.CurrentShield = MathF.Min(stats.MaxShield, stats.CurrentShield + stats.ShieldRechargeRate * deltaTimeSeconds);
            }

            // Energy Recharge
            if (stats.CurrentEnergy < stats.MaxEnergy)
            {
                stats.CurrentEnergy = MathF.Min(stats.MaxEnergy, stats.CurrentEnergy + stats.EnergyRechargeRate * deltaTimeSeconds);
            }
        }

        // 2. Handle dead drones
        var toDestroy = new List<int>();
        foreach (var entity in world.Query<DroneStatsComponent, TransformComponent>())
        {
            var stats = world.GetComponent<DroneStatsComponent>(entity);
            if (stats.CurrentHealth <= 0f)
            {
                toDestroy.Add(entity);
            }
        }

        for (var i = 0; i < toDestroy.Count; i++)
        {
            var droneEntity = toDestroy[i];
            
            // Drop any package the drone was carrying
            if (world.HasComponent<DroneCarryComponent>(droneEntity))
            {
                var carry = world.GetComponent<DroneCarryComponent>(droneEntity);
                if (carry.CarriedPackageEntity != -1 && world.HasComponent<PackageComponent>(carry.CarriedPackageEntity))
                {
                    var packageEntity = carry.CarriedPackageEntity;
                    var package = world.GetComponent<PackageComponent>(packageEntity);
                    var packageTransform = world.GetComponent<TransformComponent>(packageEntity);
                    var droneTransform = world.GetComponent<TransformComponent>(droneEntity);
                    
                    var packageHalfHeight = 0.21f; // Default height backup
                    if (world.HasComponent<PackageRenderComponent>(packageEntity))
                    {
                        var packageRender = world.GetComponent<PackageRenderComponent>(packageEntity);
                        packageHalfHeight = packageRender.Size.Y * 0.5f;
                    }

                    // Drop package on the ground at the drone's position
                    var dropPos = new Vector3(droneTransform.Position.X, packageHalfHeight, droneTransform.Position.Z);
                    packageTransform.Position = dropPos;
                    packageTransform.BasePosition = dropPos;
                    package.IsCarried = false;
                    package.CarrierEntity = -1;
                }
            }

            world.DestroyEntity(droneEntity);
        }
    }
}
