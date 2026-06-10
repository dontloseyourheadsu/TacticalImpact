using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs;
using TacticalImpact.MonoGame.Ecs.Components;
using TacticalImpact.MonoGame.Ecs.Systems;

namespace TacticalImpact.MonoGame.Features.Playground;

public sealed class PlaygroundFeatureModule
{
    public DroneSelectionSystem SelectionSystem { get; } = new();
    public DroneCommandSystem CommandSystem { get; } = new(2f);
    public DronePackageCarrySystem PackageCarrySystem { get; } = new();
    public DroneShootingSystem ShootingSystem { get; } = new();

    public IReadOnlyList<ISystem> CreateSystems()
    {
        return
        [
            SelectionSystem,
            CommandSystem,
            PackageCarrySystem,
            ShootingSystem,
            new DroneGroupMovementSystem(),
            new DronePhysicsSystem(),
            new DroneCollisionResolutionSystem(),
            new DroneZoneDetectionSystem(),
            new ProjectileMovementSystem(),
            new ProjectileLifetimeSystem(),
            new ProjectileCollisionSystem(),
            new DroneStatsUpdateSystem()
        ];
    }

    public void InitializeEntities(EcsWorld world)
    {
        SpawnDrone(world, new Vector3(-3f, 2f, 0f), 0f, 0); // Scout drone (Type 0)
        SpawnDrone(world, new Vector3(0f, 2f, 0f), 0f, 1);  // Assault drone (Type 1)
        SpawnDrone(world, new Vector3(3f, 2f, 0f), 0f, 2);  // Heavy Tank drone (Type 2)

        SpawnPackage(world, new Vector3(-5f, 0f, 3f));
        SpawnPackage(world, new Vector3(0f, 0f, 5f));
        SpawnPackage(world, new Vector3(5f, 0f, -2f));

        SpawnZone(world, new Vector3(0f, 0f, 0f), 2f, new Color(79, 177, 255), 1.0f);
        SpawnZone(world, new Vector3(-8f, 0f, 4f), 3.2f, new Color(109, 236, 143), 1.15f);
        SpawnZone(world, new Vector3(8f, 0f, -5f), 1.25f, new Color(255, 182, 74), 0.9f);
    }

    private static void SpawnDrone(EcsWorld world, Vector3 startPosition, float timeOffset, int droneType)
    {
        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            BasePosition = startPosition,
            Position = startPosition
        });
        world.AddComponent(entity, new DroneTargetComponent
        {
            TargetPosition = startPosition
        });

        // Set up base motion variables (will be adjusted per-class)
        var motion = new DroneMotionComponent
        {
            MoveDistance = 0.14f,
            MoveSpeed = 0.72f,
            FloatAmplitude = 0.07f,
            FloatSpeed = 1.45f,
            TimeOffset = timeOffset
        };
        world.AddComponent(entity, motion);

        var physics = new DronePhysicsComponent
        {
            PositionGain = 3.2f,
            VelocityGain = 8.4f,
            LinearDamping = 2.8f
        };
        world.AddComponent(entity, physics);

        var render = new DroneRenderComponent();
        world.AddComponent(entity, render);

        var collision = new CollisionComponent
        {
            Layer = 1,
            CollidesWithMask = 1,
            Radius = 0.55f
        };
        world.AddComponent(entity, collision);

        world.AddComponent(entity, new DroneSelectionComponent
        {
            IsSelected = false
        });
        world.AddComponent(entity, new DroneZoneStatusComponent());
        world.AddComponent(entity, new DroneCarryComponent
        {
            CarryTargetPosition = startPosition
        });

        var weapon = new WeaponComponent();
        world.AddComponent(entity, weapon);

        var stats = new DroneStatsComponent();
        world.AddComponent(entity, stats);

        var sensor = new DroneSensorComponent();
        world.AddComponent(entity, sensor);

        // Customize components based on drone type
        if (droneType == 0) // Scout drone
        {
            render.BaseColor = new Color(230, 190, 45); // Yellow/Gold
            render.Size = 0.65f;
            collision.Radius = 0.45f;

            stats.MaxHealth = 60f;
            stats.CurrentHealth = 60f;
            stats.MaxShield = 20f;
            stats.CurrentShield = 20f;
            stats.Weight = 0.45f; // Light weight
            stats.WindResistance = 1.2f; // High wind surface relative to weight

            sensor.HasLidarBackup = true; // Has lidar backup, can operate while blinded
            sensor.LidarRange = 5f;
            sensor.OpticalRange = 10f;

            weapon.Type = WeaponType.BlindingLaser;
            weapon.CooldownSeconds = 0.15f; // Shoots fast blinding beams
            weapon.ProjectileSpeed = 24f;
            weapon.ProjectileLifeSeconds = 1.2f;
            weapon.Damage = 2f;
            weapon.BlindDuration = 4.0f; // Long blind duration
            weapon.MuzzleHeight = 0.25f;

            motion.MoveSpeed = 1.2f;
            motion.FloatSpeed = 2.2f;
            physics.PositionGain = 4.5f;
            physics.VelocityGain = 12f;
        }
        else if (droneType == 1) // Assault drone
        {
            render.BaseColor = new Color(79, 177, 255); // Blue
            render.Size = 0.8f;
            collision.Radius = 0.55f;

            stats.MaxHealth = 100f;
            stats.CurrentHealth = 100f;
            stats.MaxShield = 50f;
            stats.CurrentShield = 50f;
            stats.Weight = 1.0f;
            stats.WindResistance = 0.5f;

            sensor.HasAntiGlareFilter = true; // Reduces blind duration by 75%
            sensor.OpticalRange = 14f;

            weapon.Type = WeaponType.PlasmaCannon;
            weapon.CooldownSeconds = 0.35f;
            weapon.ProjectileSpeed = 17f;
            weapon.ProjectileLifeSeconds = 2.0f;
            weapon.Damage = 12f;
            weapon.MuzzleHeight = 0.35f;
        }
        else // Heavy Tank drone (droneType == 2)
        {
            render.BaseColor = new Color(235, 75, 75); // Red
            render.Size = 1.05f;
            collision.Radius = 0.72f;

            stats.MaxHealth = 200f;
            stats.CurrentHealth = 200f;
            stats.MaxShield = 100f;
            stats.CurrentShield = 100f;
            stats.Weight = 2.5f; // Heavy weight
            stats.WindResistance = 0.2f; // Low wind susceptibility

            // No special sensor upgrades
            sensor.OpticalRange = 16f;

            weapon.Type = WeaponType.PlasmaCannon;
            weapon.CooldownSeconds = 0.95f; // Slow but deadly
            weapon.ProjectileSpeed = 13f;
            weapon.ProjectileLifeSeconds = 2.5f;
            weapon.Damage = 35f; // Massive damage
            weapon.MuzzleHeight = 0.45f;

            motion.MoveSpeed = 0.45f;
            motion.FloatSpeed = 1.0f;
            physics.PositionGain = 2.2f;
            physics.VelocityGain = 6.0f;
            physics.LinearDamping = 3.5f;
        }
    }

    private static void SpawnPackage(EcsWorld world, Vector3 groundPosition)
    {
        var render = new PackageRenderComponent();
        var packageHalfHeight = render.Size.Y * 0.5f;
        var placedPosition = new Vector3(groundPosition.X, packageHalfHeight, groundPosition.Z);

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            BasePosition = placedPosition,
            Position = placedPosition
        });
        world.AddComponent(entity, new PackageComponent());
        world.AddComponent(entity, new PackageZoneStatusComponent());
        world.AddComponent(entity, render);
    }

    private static void SpawnZone(EcsWorld world, Vector3 center, float radius, Color color, float height)
    {
        var entity = world.CreateEntity();
        world.AddComponent(entity, new ZoneComponent
        {
            Center = new Vector3(center.X, 0f, center.Z),
            Radius = radius,
            HoverMinHeight = 0.75f,
            HologramHeight = height,
            HologramColor = color
        });
    }
}