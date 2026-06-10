using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TacticalImpact.MonoGame.Ecs;
using TacticalImpact.MonoGame.Ecs.Components;
using TacticalImpact.MonoGame.Ecs.Systems;
using TacticalImpact.MonoGame.Features.Playground;
using TacticalImpact.MonoGame.Rendering;

namespace TacticalImpact.MonoGame.Modes;

public sealed class PlaygroundMode : IGameMode
{
    private readonly TacticalImpactGame _game;
    private readonly EcsWorld _world = new();
    private readonly PlaygroundFeatureModule _featureModule = new();
    private readonly OrbitCamera3D _camera = new();
    private readonly List<ISystem> _systems = [];
    private readonly PrimitiveRenderer _primitiveRenderer = new();

    private BasicEffect? _effect;
    private SpriteBatch? _spriteBatch;
    private VertexPositionColor[] _gridVertices = [];
    private Vector3[] _unitSpherePositions = [];
    private short[] _unitSphereIndices = [];
    private bool _loaded;
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyboardState;
    private double _totalTimeSeconds;
    private double _lastLeftClickTimeSeconds = -5d;
    private Point _lastLeftClickPosition;

    public PlaygroundMode(TacticalImpactGame game)
    {
        _game = game;
    }

    public void Initialize()
    {
        _systems.Clear();
        _systems.AddRange(_featureModule.CreateSystems());
        _featureModule.InitializeEntities(_world);
        _featureModule.SelectionSystem.ClearSelection(_world);
    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _effect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _primitiveRenderer.Initialize(graphicsDevice);

        _gridVertices = BuildGridVertices(30, 1f, new Color(35, 55, 110));
        BuildUnitSphereMesh(8, 12, out _unitSpherePositions, out _unitSphereIndices);
        _loaded = true;
    }

    public void Update(GameTime gameTime)
    {
        if (!_loaded)
        {
            return;
        }

        _totalTimeSeconds = gameTime.TotalGameTime.TotalSeconds;
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach (var system in _systems)
        {
            system.Update(_world, deltaTime);
        }

        _camera.Update(gameTime, _game.GraphicsDevice.Viewport);
        HandleInteractionInput();
    }

    public void Draw(GameTime gameTime, GraphicsDevice graphicsDevice)
    {
        if (!_loaded || _effect is null)
        {
            return;
        }

        graphicsDevice.Clear(new Color(10, 15, 33));

        graphicsDevice.DepthStencilState = DepthStencilState.Default;
        graphicsDevice.RasterizerState = RasterizerState.CullNone;

        _effect.View = _camera.View;
        _effect.Projection = _camera.Projection;

        _effect.World = Matrix.Identity;
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(
                PrimitiveType.LineList,
                _gridVertices,
                0,
                _gridVertices.Length / 2);
        }

        DrawDrones3D(graphicsDevice, _effect);
        DrawZones3D(graphicsDevice, _effect);
        DrawDroneGroundIndicators3D(graphicsDevice, _effect);
        DrawPackages3D(graphicsDevice, _effect);
        DrawProjectiles3D(graphicsDevice, _effect);
        DrawExplosions3D(graphicsDevice, _effect);
        DrawDroneScreenMarkers(graphicsDevice);
        DrawPackageScreenMarkers(graphicsDevice);
    }

    public void Dispose()
    {
        _effect?.Dispose();
        _effect = null;
        _spriteBatch?.Dispose();
        _spriteBatch = null;
        _primitiveRenderer.Dispose();
    }

    private void DrawDrones3D(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        foreach (var entity in _world.Query<TransformComponent, DroneRenderComponent, DroneSelectionComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            var render = _world.GetComponent<DroneRenderComponent>(entity);
            var selection = _world.GetComponent<DroneSelectionComponent>(entity);

            var world = Matrix.CreateScale(render.Size) * Matrix.CreateTranslation(transform.Position);
            effect.World = world;

            var intensity = MathHelper.Clamp((transform.Position.Y + 2f) / 4f, 0f, 1f);
            var baseColor = selection.IsSelected
                ? Color.LimeGreen
                : render.BaseColor;
            var color = Color.Lerp(baseColor, Color.White, intensity * 0.45f);
            var cubeVertices = BuildUnitCubeVertices(color);
            var cubeIndices = GetUnitCubeIndices();

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    cubeVertices,
                    0,
                    cubeVertices.Length,
                    cubeIndices,
                    0,
                    cubeIndices.Length / 3);
            }
        }
    }

    private void HandleInteractionInput()
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var increase = keyboard.IsKeyDown(Keys.R);
        var decrease = keyboard.IsKeyDown(Keys.F);

        if (increase || decrease)
        {
            const float delta = 0.05f;
            var amount = increase ? delta : -delta;

            foreach (var entity in _world.Query<DroneMotionComponent>())
            {
                var motion = _world.GetComponent<DroneMotionComponent>(entity);
                motion.MoveDistance = MathHelper.Clamp(motion.MoveDistance + amount, 1.0f, 10.0f);
            }
        }

        if (mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            ProcessLeftClick(mouse.Position, IsDoubleClick(mouse.Position));
        }

        if (mouse.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
        {
            ProcessRightClick(mouse.Position);
        }

        if (mouse.MiddleButton == ButtonState.Pressed && _previousMouseState.MiddleButton == ButtonState.Released)
        {
            _featureModule.SelectionSystem.ClearSelection(_world);
        }

        // Weapon Switching Controls
        if (PressedNow(keyboard, Keys.D1)) SwitchSelectedDroneWeapon(WeaponType.PlasmaCannon);
        if (PressedNow(keyboard, Keys.D2)) SwitchSelectedDroneWeapon(WeaponType.BlindingLaser);
        if (PressedNow(keyboard, Keys.D3)) SwitchSelectedDroneWeapon(WeaponType.MachineGun);
        if (PressedNow(keyboard, Keys.D4)) SwitchSelectedDroneWeapon(WeaponType.MissileLauncher);
        if (PressedNow(keyboard, Keys.D5)) SwitchSelectedDroneWeapon(WeaponType.GrenadeLauncher);

        // Upgrading selected drone & weapon
        if (PressedNow(keyboard, Keys.U)) UpgradeSelectedDrone();
        if (PressedNow(keyboard, Keys.I)) UpgradeSelectedWeapon();

        // Manual Reload trigger
        if (PressedNow(keyboard, Keys.Y))
        {
            var selected = GetSelectedDroneEntity();
            if (selected != -1)
            {
                _featureModule.ShootingSystem.TriggerReload(_world, selected);
            }
        }

        _previousMouseState = mouse;
        _previousKeyboardState = keyboard;
    }

    private bool PressedNow(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
    }

    private void SwitchSelectedDroneWeapon(WeaponType type)
    {
        var selected = GetSelectedDroneEntity();
        if (selected == -1 || !_world.HasComponent<WeaponComponent>(selected))
        {
            return;
        }

        var weapon = _world.GetComponent<WeaponComponent>(selected);
        weapon.Type = type;

        // Reset base weapon stats but retain upgrade levels
        switch (type)
        {
            case WeaponType.PlasmaCannon:
                weapon.CooldownSeconds = 0.35f;
                weapon.ProjectileSpeed = 16f;
                weapon.ProjectileLifeSeconds = 2.25f;
                weapon.Damage = 15f;
                weapon.MaxAmmo = 10;
                weapon.ReloadTimeSeconds = 2.0f;
                break;
            case WeaponType.BlindingLaser:
                weapon.CooldownSeconds = 0.15f;
                weapon.ProjectileSpeed = 24f;
                weapon.ProjectileLifeSeconds = 1.2f;
                weapon.Damage = 2f;
                weapon.BlindDuration = 4.0f;
                weapon.MaxAmmo = 15;
                weapon.ReloadTimeSeconds = 1.5f;
                break;
            case WeaponType.MachineGun:
                weapon.CooldownSeconds = 0.08f;
                weapon.ProjectileSpeed = 22f;
                weapon.ProjectileLifeSeconds = 1.0f;
                weapon.Damage = 4f;
                weapon.MaxAmmo = 50;
                weapon.ReloadTimeSeconds = 2.5f;
                break;
            case WeaponType.MissileLauncher:
                weapon.CooldownSeconds = 1.2f;
                weapon.ProjectileSpeed = 10f;
                weapon.ProjectileLifeSeconds = 3.5f;
                weapon.Damage = 25f;
                weapon.MaxAmmo = 3;
                weapon.ReloadTimeSeconds = 3.5f;
                break;
            case WeaponType.GrenadeLauncher:
                weapon.CooldownSeconds = 0.75f;
                weapon.ProjectileSpeed = 12f;
                weapon.ProjectileLifeSeconds = 2.0f;
                weapon.Damage = 20f;
                weapon.MaxAmmo = 5;
                weapon.ReloadTimeSeconds = 3.0f;
                break;
        }

        // Apply stats scaling multipliers based on upgrades
        for (var i = 1; i < weapon.DamageUpgradeLevel; i++)
        {
            weapon.Damage = MathF.Round(weapon.Damage * 1.25f, 1);
            if (weapon.Type == WeaponType.BlindingLaser)
            {
                weapon.BlindDuration *= 1.15f;
            }
        }
        for (var i = 1; i < weapon.FireRateUpgradeLevel; i++)
        {
            weapon.CooldownSeconds = MathF.Max(0.05f, weapon.CooldownSeconds * 0.82f);
        }
        for (var i = 1; i < weapon.AmmoUpgradeLevel; i++)
        {
            weapon.MaxAmmo = (int)MathF.Ceiling(weapon.MaxAmmo * 1.3f);
            weapon.ReloadTimeSeconds = MathF.Max(0.5f, weapon.ReloadTimeSeconds * 0.88f);
        }

        weapon.CurrentAmmo = weapon.MaxAmmo;
        weapon.IsReloading = false;
        weapon.CooldownRemaining = 0f;
    }

    private void UpgradeSelectedDrone()
    {
        var selected = GetSelectedDroneEntity();
        if (selected == -1 || !_world.HasComponent<DroneStatsComponent>(selected))
        {
            return;
        }

        var stats = _world.GetComponent<DroneStatsComponent>(selected);
        stats.UpgradeHealth();
        stats.UpgradeShield();
        stats.UpgradeArmor();
        stats.UpgradeEngine(_world, selected);
    }

    private void UpgradeSelectedWeapon()
    {
        var selected = GetSelectedDroneEntity();
        if (selected == -1 || !_world.HasComponent<WeaponComponent>(selected))
        {
            return;
        }

        var weapon = _world.GetComponent<WeaponComponent>(selected);
        weapon.UpgradeDamage();
        weapon.UpgradeFireRate();
        weapon.UpgradeAmmo();
        
        // Also refill ammo
        weapon.CurrentAmmo = weapon.MaxAmmo;
    }

    private void ProcessLeftClick(Point pointerPosition, bool isDoubleClick)
    {
        var viewport = _game.GraphicsDevice.Viewport;
        var nearPoint = viewport.Unproject(
            new Vector3(pointerPosition.X, pointerPosition.Y, 0f),
            _camera.Projection,
            _camera.View,
            Matrix.Identity);
        var farPoint = viewport.Unproject(
            new Vector3(pointerPosition.X, pointerPosition.Y, 1f),
            _camera.Projection,
            _camera.View,
            Matrix.Identity);

        var rayDirection = farPoint - nearPoint;
        if (rayDirection.LengthSquared() <= float.Epsilon)
        {
            return;
        }

        var ray = new Ray(nearPoint, Vector3.Normalize(rayDirection));

        if (DronePickingHelper.TryPickDrone(_world, ray, out var pickedDrone))
        {
            _featureModule.SelectionSystem.Select(_world, pickedDrone);
            return;
        }

        var selectedDrone = GetSelectedDroneEntity();
        if (selectedDrone != -1 &&
            DronePickingHelper.TryPickPackage(_world, ray, out var pickedPackage) &&
            !_featureModule.PackageCarrySystem.IsDroneCarryingPackage(_world, selectedDrone))
        {
            _featureModule.PackageCarrySystem.QueuePickupCommand(selectedDrone, pickedPackage);
            return;
        }

        if (DronePickingHelper.TryIntersectGround(ray, out var groundPoint))
        {
            if (selectedDrone != -1 && _featureModule.PackageCarrySystem.IsDroneCarryingPackage(_world, selectedDrone))
            {
                _featureModule.PackageCarrySystem.QueueCarryMoveCommand(
                    groundPoint,
                    true,
                    isDoubleClick);
                return;
            }

            _featureModule.CommandSystem.QueueMoveCommand(
                groundPoint,
                _featureModule.SelectionSystem.HasSelection);
        }
    }

    private bool IsDoubleClick(Point clickPosition)
    {
        var clickDeltaTime = _totalTimeSeconds - _lastLeftClickTimeSeconds;
        var clickDeltaX = clickPosition.X - _lastLeftClickPosition.X;
        var clickDeltaY = clickPosition.Y - _lastLeftClickPosition.Y;
        var closeEnough = clickDeltaX * clickDeltaX + clickDeltaY * clickDeltaY <= 24 * 24;
        var isDoubleClick = clickDeltaTime <= 0.35d && closeEnough;

        _lastLeftClickTimeSeconds = _totalTimeSeconds;
        _lastLeftClickPosition = clickPosition;

        return isDoubleClick;
    }

    private int GetSelectedDroneEntity()
    {
        foreach (var entity in _world.Query<DroneSelectionComponent>())
        {
            var selection = _world.GetComponent<DroneSelectionComponent>(entity);
            if (selection.IsSelected)
            {
                return entity;
            }
        }

        return -1;
    }

    private void DrawPackages3D(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        foreach (var entity in _world.Query<TransformComponent, PackageRenderComponent, PackageComponent>())
        {
            if (!_world.HasComponent<PackageZoneStatusComponent>(entity))
            {
                continue;
            }

            var transform = _world.GetComponent<TransformComponent>(entity);
            var render = _world.GetComponent<PackageRenderComponent>(entity);
            var package = _world.GetComponent<PackageComponent>(entity);
            var zoneStatus = _world.GetComponent<PackageZoneStatusComponent>(entity);

            var world = Matrix.CreateScale(render.Size) * Matrix.CreateTranslation(transform.Position);
            effect.World = world;

            var baseColor = package.IsCarried
                ? Color.Lerp(render.BaseColor, Color.LightGoldenrodYellow, 0.6f)
                : render.BaseColor;
            if (zoneStatus.IsInZone)
            {
                baseColor = Color.Lerp(baseColor, new Color(134, 255, 154), 0.5f);
            }

            var cubeVertices = BuildUnitCubeVertices(baseColor);
            var cubeIndices = GetUnitCubeIndices();

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    cubeVertices,
                    0,
                    cubeVertices.Length,
                    cubeIndices,
                    0,
                    cubeIndices.Length / 3);
            }
        }
    }

    private void DrawZones3D(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        var previousBlend = graphicsDevice.BlendState;
        var previousDepth = graphicsDevice.DepthStencilState;

        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        foreach (var entity in _world.Query<ZoneComponent>())
        {
            var zone = _world.GetComponent<ZoneComponent>(entity);
            var segments = Math.Max(20, (int)(zone.Radius * 20f));
            var center = zone.Center;

            var rings = new[]
            {
                (zone.Radius, 0.03f, zone.HologramColor * 0.95f),
                (zone.Radius * 0.72f, 0.06f, zone.HologramColor * 0.6f),
                (zone.Radius * 0.46f, 0.09f, zone.HologramColor * 0.35f)
            };

            for (var i = 0; i < rings.Length; i++)
            {
                var circle = BuildCircleLineListVertices(center, rings[i].Item1, rings[i].Item2, rings[i].Item3, segments);
                effect.World = Matrix.Identity;
                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        circle,
                        0,
                        circle.Length / 2);
                }
            }

            var beamVertices = BuildZoneBeamVertices(zone, 10);
            effect.World = Matrix.Identity;
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(
                    PrimitiveType.LineList,
                    beamVertices,
                    0,
                    beamVertices.Length / 2);
            }
        }

        graphicsDevice.DepthStencilState = previousDepth;
        graphicsDevice.BlendState = previousBlend;
    }

    private void DrawDroneGroundIndicators3D(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        foreach (var entity in _world.Query<TransformComponent, DroneZoneStatusComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            var zoneStatus = _world.GetComponent<DroneZoneStatusComponent>(entity);

            var lineColor = zoneStatus.IsInZone ? Color.LawnGreen : new Color(255, 118, 118);
            var ground = new Vector3(transform.Position.X, 0.02f, transform.Position.Z);
            var vertices = new[]
            {
                new VertexPositionColor(transform.Position, lineColor * 0.8f),
                new VertexPositionColor(ground, lineColor * 0.8f)
            };

            effect.World = Matrix.Identity;
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(
                    PrimitiveType.LineList,
                    vertices,
                    0,
                    1);
            }
        }
    }

    private void DrawBorderedRectangle(SpriteBatch spriteBatch, Rectangle rect, int borderThickness, Color fillColor, Color borderColor)
    {
        _primitiveRenderer.FillRectangle(spriteBatch, rect, borderColor);
        var innerRect = new Rectangle(
            rect.X + borderThickness,
            rect.Y + borderThickness,
            rect.Width - borderThickness * 2,
            rect.Height - borderThickness * 2);
        _primitiveRenderer.FillRectangle(spriteBatch, innerRect, fillColor);
    }

    private void DrawDroneScreenMarkers(GraphicsDevice graphicsDevice)
    {
        if (_spriteBatch is null)
        {
            return;
        }

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        var viewport = graphicsDevice.Viewport;

        foreach (var entity in _world.Query<TransformComponent, DroneZoneStatusComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            var zoneStatus = _world.GetComponent<DroneZoneStatusComponent>(entity);
            var markerColor = zoneStatus.IsInZone ? new Color(145, 255, 154) : new Color(255, 142, 142);

            var projected = viewport.Project(transform.Position, _camera.Projection, _camera.View, Matrix.Identity);
            if (projected.Z is < 0f or > 1f)
            {
                continue;
            }

            var marker = new Rectangle((int)projected.X - 4, (int)projected.Y - 4, 8, 8);
            _primitiveRenderer.FillRectangle(_spriteBatch, marker, markerColor * 0.95f);

            // Draw Health, Shield, and Blinding status HUD indicators above the drone
            if (_world.HasComponent<DroneStatsComponent>(entity))
            {
                var stats = _world.GetComponent<DroneStatsComponent>(entity);
                
                var barWidth = 36;
                var barHeight = 4;
                var startX = (int)projected.X - barWidth / 2;
                var startY = (int)projected.Y - 18;

                // 1. Health Bar
                var healthBg = new Rectangle(startX, startY, barWidth, barHeight);
                var healthFillWidth = (int)(barWidth * MathHelper.Clamp(stats.CurrentHealth / stats.MaxHealth, 0f, 1f));
                var healthFill = new Rectangle(startX, startY, healthFillWidth, barHeight);
                
                _primitiveRenderer.FillRectangle(_spriteBatch, healthBg, new Color(40, 40, 40) * 0.8f);
                _primitiveRenderer.FillRectangle(_spriteBatch, healthFill, new Color(50, 220, 80));

                // 2. Shield Bar (only if drone has shields)
                if (stats.MaxShield > 0f)
                {
                    var shieldBg = new Rectangle(startX, startY - 5, barWidth, barHeight);
                    var shieldFillWidth = (int)(barWidth * MathHelper.Clamp(stats.CurrentShield / stats.MaxShield, 0f, 1f));
                    var shieldFill = new Rectangle(startX, startY - 5, shieldFillWidth, barHeight);

                    _primitiveRenderer.FillRectangle(_spriteBatch, shieldBg, new Color(40, 40, 40) * 0.8f);
                    _primitiveRenderer.FillRectangle(_spriteBatch, shieldFill, new Color(50, 150, 255));
                }

                // 3. Sensor Blinding / Lidar Warning indicator
                if (_world.HasComponent<DroneSensorComponent>(entity))
                {
                    var sensor = _world.GetComponent<DroneSensorComponent>(entity);
                    if (sensor.BlindedDurationRemaining > 0f)
                    {
                        var indicatorY = startY - 11;
                        if (sensor.ActiveSensor == SensorType.LidarScanner)
                        {
                            // Lidar backup active: Draw small violet indicator dot
                            var backupIndicator = new Rectangle((int)projected.X - 3, indicatorY, 6, 6);
                            _primitiveRenderer.FillRectangle(_spriteBatch, backupIndicator, new Color(180, 80, 255));
                        }
                        else if (sensor.IsFullyBlinded)
                        {
                            // Fully blinded: Draw flashing yellow warning rectangle
                            var isFlashOn = ((int)(_totalTimeSeconds * 6f) % 2) == 0;
                            var warningColor = isFlashOn ? new Color(255, 230, 40) : new Color(120, 100, 20);
                            
                            var warningRect = new Rectangle((int)projected.X - 3, indicatorY, 6, 6);
                            _primitiveRenderer.FillRectangle(_spriteBatch, warningRect, warningColor);
                        }
                    }
                }
            }

            var projectedGround = viewport.Project(
                new Vector3(transform.Position.X, 0f, transform.Position.Z),
                _camera.Projection,
                _camera.View,
                Matrix.Identity);

            if (projectedGround.Z is >= 0f and <= 1f)
            {
                var groundMarker = new Rectangle((int)projectedGround.X - 3, (int)projectedGround.Y - 3, 6, 6);
                _primitiveRenderer.FillRectangle(_spriteBatch, groundMarker, markerColor * 0.6f);
            }
        }

        // Draw Wind HUD
        DrawWindHUD(graphicsDevice);

        // Draw Selected Drone HUD
        DrawSelectedDroneHUD(graphicsDevice);

        _spriteBatch.End();
    }

    private void DrawSelectedDroneHUD(GraphicsDevice graphicsDevice)
    {
        var selected = GetSelectedDroneEntity();
        if (selected == -1 || _spriteBatch is null)
        {
            return;
        }

        var viewport = graphicsDevice.Viewport;
        var boxWidth = 240;
        var boxHeight = 110;
        var x = 10;
        var y = viewport.Height - boxHeight - 10;

        var bgRect = new Rectangle(x, y, boxWidth, boxHeight);
        DrawBorderedRectangle(_spriteBatch, bgRect, 1, new Color(12, 16, 26) * 0.9f, new Color(45, 120, 200));

        // Draw weapon color tag
        Color weaponColor = Color.Gray;
        if (_world.HasComponent<WeaponComponent>(selected))
        {
            var weapon = _world.GetComponent<WeaponComponent>(selected);
            switch (weapon.Type)
            {
                case WeaponType.PlasmaCannon: weaponColor = new Color(255, 110, 50); break;
                case WeaponType.BlindingLaser: weaponColor = new Color(255, 245, 120); break;
                case WeaponType.MachineGun: weaponColor = new Color(200, 240, 255); break;
                case WeaponType.MissileLauncher: weaponColor = new Color(100, 255, 120); break;
                case WeaponType.GrenadeLauncher: weaponColor = new Color(170, 170, 170); break;
            }
        }
        _primitiveRenderer.FillRectangle(_spriteBatch, new Rectangle(x + 10, y + 10, 10, 10), weaponColor);

        // 1. Health and Shield Bars
        if (_world.HasComponent<DroneStatsComponent>(selected))
        {
            var stats = _world.GetComponent<DroneStatsComponent>(selected);
            
            var barW = 185;
            var barH = 6;
            var barX = x + 30;
            
            // Health
            _primitiveRenderer.FillRectangle(_spriteBatch, new Rectangle(barX, y + 11, barW, barH), new Color(40, 40, 40));
            var hpPercent = MathHelper.Clamp(stats.CurrentHealth / stats.MaxHealth, 0f, 1f);
            _primitiveRenderer.FillRectangle(_spriteBatch, new Rectangle(barX, y + 11, (int)(barW * hpPercent), barH), new Color(50, 220, 80));

            // Shield
            _primitiveRenderer.FillRectangle(_spriteBatch, new Rectangle(barX, y + 21, barW, barH), new Color(40, 40, 40));
            if (stats.MaxShield > 0f)
            {
                var shPercent = MathHelper.Clamp(stats.CurrentShield / stats.MaxShield, 0f, 1f);
                _primitiveRenderer.FillRectangle(_spriteBatch, new Rectangle(barX, y + 21, (int)(barW * shPercent), barH), new Color(50, 150, 255));
            }

            // 2. Upgrades HUD display
            // Draw drone upgrade levels (Health, Shield, Armor, Engine) as green/blue/yellow dots
            var dotY = y + 36;
            // Label box: green
            _primitiveRenderer.FillRectangle(_spriteBatch, new Rectangle(x + 10, dotY + 2, 10, 6), new Color(100, 255, 150));
            
            // Health dots
            DrawLevelDots(x + 30, dotY, stats.HealthUpgradeLevel, new Color(50, 220, 80));
            // Shield dots
            DrawLevelDots(x + 80, dotY, stats.ShieldUpgradeLevel, new Color(50, 150, 255));
            // Armor dots
            DrawLevelDots(x + 130, dotY, stats.ArmorUpgradeLevel, new Color(200, 200, 100));
            // Engine dots
            DrawLevelDots(x + 180, dotY, stats.EngineUpgradeLevel, new Color(255, 100, 100));
        }

        // 3. Weapon Stats & Ammo / Reload progress
        if (_world.HasComponent<WeaponComponent>(selected))
        {
            var weapon = _world.GetComponent<WeaponComponent>(selected);
            var ammoY = y + 54;
            
            // Draw weapon upgrades (Damage, FireRate, Ammo) as orange dots
            // Label box: orange
            _primitiveRenderer.FillRectangle(_spriteBatch, new Rectangle(x + 10, ammoY + 2, 10, 6), new Color(255, 180, 100));
            
            DrawLevelDots(x + 30, ammoY, weapon.DamageUpgradeLevel, new Color(255, 110, 50));
            DrawLevelDots(x + 80, ammoY, weapon.FireRateUpgradeLevel, new Color(255, 230, 80));
            DrawLevelDots(x + 130, ammoY, weapon.AmmoUpgradeLevel, new Color(145, 235, 255));

            // Reload / Ammunition display
            var reloadY = y + 72;
            if (weapon.IsReloading)
            {
                // Reload progress bar
                _primitiveRenderer.FillRectangle(_spriteBatch, new Rectangle(x + 30, reloadY + 2, 185, 6), new Color(40, 40, 40));
                var reloadProgress = MathHelper.Clamp(1f - (weapon.ReloadRemainingSeconds / weapon.ReloadTimeSeconds), 0f, 1f);
                _primitiveRenderer.FillRectangle(_spriteBatch, new Rectangle(x + 30, reloadY + 2, (int)(185 * reloadProgress), 6), new Color(255, 240, 100));
            }
            else
            {
                // Draw ammunition tick lines
                var ammoW = 3;
                var spacing = 2;
                var totalW = weapon.MaxAmmo * (ammoW + spacing) - spacing;
                
                // Keep it bounded
                if (totalW > 185)
                {
                    ammoW = Math.Max(1, 185 / weapon.MaxAmmo - 1);
                    spacing = 1;
                }

                for (var i = 0; i < weapon.MaxAmmo; i++)
                {
                    var drawX = x + 30 + i * (ammoW + spacing);
                    var col = i < weapon.CurrentAmmo ? weaponColor : new Color(50, 50, 50);
                    _primitiveRenderer.FillRectangle(_spriteBatch, new Rectangle(drawX, reloadY, ammoW, 8), col);
                }
            }
        }
    }

    private void DrawLevelDots(int startX, int startY, int level, Color color)
    {
        // Draw up to 5 little square dots representing upgrade level
        var maxDots = Math.Min(5, level);
        for (var i = 0; i < maxDots; i++)
        {
            _primitiveRenderer.FillRectangle(_spriteBatch!, new Rectangle(startX + i * 8, startY + 2, 5, 5), color);
        }
    }

    private void DrawWindHUD(GraphicsDevice graphicsDevice)
    {
        var viewport = graphicsDevice.Viewport;
        var boxWidth = 140;
        var boxHeight = 60;
        var x = viewport.Width - boxWidth - 10;
        var y = 10;

        var backgroundRect = new Rectangle(x, y, boxWidth, boxHeight);
        DrawBorderedRectangle(_spriteBatch!, backgroundRect, 1, new Color(15, 20, 35) * 0.85f, new Color(45, 75, 130));

        var wind = Ecs.Systems.DronePhysicsSystem.CurrentWind;

        // Draw compass circle in the box
        var centerX = x + boxWidth - 35;
        var centerY = y + boxHeight / 2;
        
        // Draw circular frame for the wind indicator using small dots
        var segments = 12;
        for (int i = 0; i < segments; i++)
        {
            var angle = MathHelper.TwoPi * i / segments;
            var dx = centerX + (int)(MathF.Cos(angle) * 16f);
            var dy = centerY + (int)(MathF.Sin(angle) * 16f);
            _primitiveRenderer.FillRectangle(_spriteBatch!, new Rectangle(dx - 1, dy - 1, 2, 2), new Color(45, 75, 130) * 0.6f);
        }

        // Draw wind arrow (XZ maps to screen XY)
        var windDir = new Vector2(wind.X, wind.Z);
        var windLen = windDir.Length();
        if (windLen > 0.05f)
        {
            windDir.Normalize();
            var arrowLength = Math.Min(16f, windLen * 3.5f);
            var endPoint = new Vector2(centerX, centerY) + windDir * arrowLength;

            // Draw line as dotted points
            var points = 12;
            for (int i = 0; i <= points; i++)
            {
                var pt = Vector2.Lerp(new Vector2(centerX, centerY), endPoint, i / (float)points);
                _primitiveRenderer.FillRectangle(_spriteBatch!, new Rectangle((int)pt.X - 1, (int)pt.Y - 1, 2, 2), new Color(100, 220, 255));
            }

            // Draw arrowhead
            _primitiveRenderer.FillRectangle(_spriteBatch!, new Rectangle((int)endPoint.X - 2, (int)endPoint.Y - 2, 4, 4), new Color(100, 220, 255));
        }

        // Draw Wind Velocity HUD gauge
        var barX = x + 12;
        var barY = y + boxHeight - 20;
        var barMaxW = 60;
        var barH = 5;
        
        var maxWindStrength = 6.0f;
        var currentWindStrengthPercent = MathHelper.Clamp(windLen / maxWindStrength, 0f, 1f);
        var currentBarW = (int)(barMaxW * currentWindStrengthPercent);

        _primitiveRenderer.FillRectangle(_spriteBatch!, new Rectangle(barX, barY, barMaxW, barH), new Color(40, 40, 40));
        _primitiveRenderer.FillRectangle(_spriteBatch!, new Rectangle(barX, barY, currentBarW, barH), new Color(100, 200, 255));
    }

    private void DrawExplosions3D(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        if (_unitSpherePositions.Length == 0 || _unitSphereIndices.Length == 0)
        {
            return;
        }

        var previousBlend = graphicsDevice.BlendState;
        var previousDepth = graphicsDevice.DepthStencilState;

        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        foreach (var entity in _world.Query<TransformComponent, ExplosionVisualComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            var explosion = _world.GetComponent<ExplosionVisualComponent>(entity);

            effect.World = Matrix.CreateScale(explosion.CurrentRadius) * Matrix.CreateTranslation(transform.Position);
            
            // Fade out as age increases
            var progress = explosion.Age / explosion.MaxAge;
            var alpha = 1f - progress;
            var color = explosion.Color * alpha;
            
            var vertices = BuildSphereVertices(color);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    vertices,
                    0,
                    vertices.Length,
                    _unitSphereIndices,
                    0,
                    _unitSphereIndices.Length / 3);
            }
        }

        graphicsDevice.DepthStencilState = previousDepth;
        graphicsDevice.BlendState = previousBlend;
    }

    private void DrawPackageScreenMarkers(GraphicsDevice graphicsDevice)
    {
        if (_spriteBatch is null)
        {
            return;
        }

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        var viewport = graphicsDevice.Viewport;

        foreach (var entity in _world.Query<TransformComponent, PackageZoneStatusComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            var zoneStatus = _world.GetComponent<PackageZoneStatusComponent>(entity);
            var markerColor = zoneStatus.IsInZone ? new Color(145, 255, 154) : new Color(255, 188, 135);

            var projected = viewport.Project(transform.Position, _camera.Projection, _camera.View, Matrix.Identity);
            if (projected.Z is < 0f or > 1f)
            {
                continue;
            }

            var marker = new Rectangle((int)projected.X - 3, (int)projected.Y - 3, 6, 6);
            _primitiveRenderer.FillRectangle(_spriteBatch, marker, markerColor * 0.88f);
        }

        _spriteBatch.End();
    }

    private void ProcessRightClick(Point pointerPosition)
    {
        var viewport = _game.GraphicsDevice.Viewport;
        var nearPoint = viewport.Unproject(
            new Vector3(pointerPosition.X, pointerPosition.Y, 0f),
            _camera.Projection,
            _camera.View,
            Matrix.Identity);
        var farPoint = viewport.Unproject(
            new Vector3(pointerPosition.X, pointerPosition.Y, 1f),
            _camera.Projection,
            _camera.View,
            Matrix.Identity);

        var rayDirection = farPoint - nearPoint;
        if (rayDirection.LengthSquared() <= float.Epsilon)
        {
            return;
        }

        var ray = new Ray(nearPoint, Vector3.Normalize(rayDirection));
        if (!DronePickingHelper.TryIntersectGround(ray, out var groundPoint))
        {
            return;
        }

        _featureModule.ShootingSystem.QueueShootCommand(
            new Vector3(groundPoint.X, 1f, groundPoint.Z),
            _featureModule.SelectionSystem.HasSelection);
    }

    private static VertexPositionColor[] BuildGridVertices(int halfExtent, float spacing, Color color)
    {
        var lineCount = (halfExtent * 2 + 1) * 2;
        var vertices = new VertexPositionColor[lineCount * 2];
        var index = 0;

        for (var i = -halfExtent; i <= halfExtent; i++)
        {
            var offset = i * spacing;
            vertices[index++] = new VertexPositionColor(new Vector3(offset, 0f, -halfExtent * spacing), color);
            vertices[index++] = new VertexPositionColor(new Vector3(offset, 0f, halfExtent * spacing), color);

            vertices[index++] = new VertexPositionColor(new Vector3(-halfExtent * spacing, 0f, offset), color);
            vertices[index++] = new VertexPositionColor(new Vector3(halfExtent * spacing, 0f, offset), color);
        }

        return vertices;
    }

    private static VertexPositionColor[] BuildUnitCubeVertices(Color color)
    {
        return
        [
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, -0.5f), color),
            new VertexPositionColor(new Vector3(0.5f, -0.5f, -0.5f), color),
            new VertexPositionColor(new Vector3(0.5f, 0.5f, -0.5f), color),
            new VertexPositionColor(new Vector3(-0.5f, 0.5f, -0.5f), color),
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0.5f), color),
            new VertexPositionColor(new Vector3(0.5f, -0.5f, 0.5f), color),
            new VertexPositionColor(new Vector3(0.5f, 0.5f, 0.5f), color),
            new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0.5f), color)
        ];
    }

    private static short[] GetUnitCubeIndices()
    {
        return
        [
            0, 1, 2, 0, 2, 3,
            4, 6, 5, 4, 7, 6,
            0, 4, 5, 0, 5, 1,
            1, 5, 6, 1, 6, 2,
            2, 6, 7, 2, 7, 3,
            3, 7, 4, 3, 4, 0
        ];
    }

    private void DrawProjectiles3D(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        if (_unitSpherePositions.Length == 0 || _unitSphereIndices.Length == 0)
        {
            return;
        }

        foreach (var entity in _world.Query<TransformComponent, ProjectileRenderComponent, ProjectileComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            var render = _world.GetComponent<ProjectileRenderComponent>(entity);

            effect.World = Matrix.CreateScale(render.Radius) * Matrix.CreateTranslation(transform.Position);
            var vertices = BuildSphereVertices(render.Color);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    vertices,
                    0,
                    vertices.Length,
                    _unitSphereIndices,
                    0,
                    _unitSphereIndices.Length / 3);
            }
        }
    }

    private VertexPositionColor[] BuildSphereVertices(Color color)
    {
        var vertices = new VertexPositionColor[_unitSpherePositions.Length];
        for (var i = 0; i < _unitSpherePositions.Length; i++)
        {
            vertices[i] = new VertexPositionColor(_unitSpherePositions[i], color);
        }

        return vertices;
    }

    private static void BuildUnitSphereMesh(int stacks, int slices, out Vector3[] positions, out short[] indices)
    {
        var pos = new List<Vector3>((stacks + 1) * (slices + 1));
        var idx = new List<short>(stacks * slices * 6);

        for (var stack = 0; stack <= stacks; stack++)
        {
            var v = stack / (float)stacks;
            var phi = MathF.PI * v;
            var y = MathF.Cos(phi);
            var r = MathF.Sin(phi);

            for (var slice = 0; slice <= slices; slice++)
            {
                var u = slice / (float)slices;
                var theta = MathHelper.TwoPi * u;
                var x = r * MathF.Cos(theta);
                var z = r * MathF.Sin(theta);
                pos.Add(new Vector3(x, y, z));
            }
        }

        for (var stack = 0; stack < stacks; stack++)
        {
            for (var slice = 0; slice < slices; slice++)
            {
                var first = (short)(stack * (slices + 1) + slice);
                var second = (short)(first + slices + 1);

                idx.Add(first);
                idx.Add(second);
                idx.Add((short)(first + 1));

                idx.Add(second);
                idx.Add((short)(second + 1));
                idx.Add((short)(first + 1));
            }
        }

        positions = [.. pos];
        indices = [.. idx];
    }

    private static VertexPositionColor[] BuildCircleLineListVertices(Vector3 center, float radius, float yOffset, Color color, int segments)
    {
        var vertices = new VertexPositionColor[segments * 2];
        var y = center.Y + yOffset;

        for (var i = 0; i < segments; i++)
        {
            var a0 = MathHelper.TwoPi * i / segments;
            var a1 = MathHelper.TwoPi * ((i + 1) % segments) / segments;

            var p0 = new Vector3(
                center.X + MathF.Cos(a0) * radius,
                y,
                center.Z + MathF.Sin(a0) * radius);
            var p1 = new Vector3(
                center.X + MathF.Cos(a1) * radius,
                y,
                center.Z + MathF.Sin(a1) * radius);

            vertices[i * 2] = new VertexPositionColor(p0, color);
            vertices[i * 2 + 1] = new VertexPositionColor(p1, color);
        }

        return vertices;
    }

    private static VertexPositionColor[] BuildZoneBeamVertices(ZoneComponent zone, int beamCount)
    {
        var vertices = new VertexPositionColor[beamCount * 2];
        var lowerColor = zone.HologramColor * 0.3f;
        var upperColor = zone.HologramColor * 0.78f;

        for (var i = 0; i < beamCount; i++)
        {
            var angle = MathHelper.TwoPi * i / beamCount;
            var x = zone.Center.X + MathF.Cos(angle) * zone.Radius;
            var z = zone.Center.Z + MathF.Sin(angle) * zone.Radius;

            vertices[i * 2] = new VertexPositionColor(new Vector3(x, 0.03f, z), lowerColor);
            vertices[i * 2 + 1] = new VertexPositionColor(new Vector3(x, zone.HologramHeight, z), upperColor);
        }

        return vertices;
    }
}