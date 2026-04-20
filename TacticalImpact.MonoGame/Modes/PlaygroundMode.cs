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

        _previousMouseState = mouse;
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

        _spriteBatch.End();
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