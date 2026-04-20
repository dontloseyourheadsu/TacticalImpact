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

    private BasicEffect? _effect;
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
        DrawPackages3D(graphicsDevice, _effect);
        DrawProjectiles3D(graphicsDevice, _effect);
    }

    public void Dispose()
    {
        _effect?.Dispose();
        _effect = null;
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
            var transform = _world.GetComponent<TransformComponent>(entity);
            var render = _world.GetComponent<PackageRenderComponent>(entity);
            var package = _world.GetComponent<PackageComponent>(entity);

            var world = Matrix.CreateScale(render.Size) * Matrix.CreateTranslation(transform.Position);
            effect.World = world;

            var baseColor = package.IsCarried
                ? Color.Lerp(render.BaseColor, Color.LightGoldenrodYellow, 0.6f)
                : render.BaseColor;

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
}