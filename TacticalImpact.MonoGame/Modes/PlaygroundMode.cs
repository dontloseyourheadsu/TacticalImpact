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
    private bool _loaded;

    public PlaygroundMode(TacticalImpactGame game)
    {
        _game = game;
    }

    public void Initialize()
    {
        _systems.Clear();
        _systems.AddRange(_featureModule.CreateSystems());
        _featureModule.InitializeEntities(_world);
    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _effect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };

        _gridVertices = BuildGridVertices(30, 1f, new Color(35, 55, 110));
        _loaded = true;
    }

    public void Update(GameTime gameTime)
    {
        if (!_loaded)
        {
            return;
        }

        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach (var system in _systems)
        {
            system.Update(_world, deltaTime);
        }

        HandleInteractionInput();
        _camera.Update(gameTime, _game.GraphicsDevice.Viewport);
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
    }

    public void Dispose()
    {
        _effect?.Dispose();
        _effect = null;
    }

    private void DrawDrones3D(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        foreach (var entity in _world.Query<TransformComponent, DroneRenderComponent>())
        {
            var transform = _world.GetComponent<TransformComponent>(entity);
            var render = _world.GetComponent<DroneRenderComponent>(entity);

            var world = Matrix.CreateScale(render.Size) * Matrix.CreateTranslation(transform.Position);
            effect.World = world;

            var intensity = MathHelper.Clamp((transform.Position.Y + 2f) / 4f, 0f, 1f);
            var color = Color.Lerp(render.BaseColor, Color.White, intensity * 0.45f);
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
        var increase = keyboard.IsKeyDown(Keys.R);
        var decrease = keyboard.IsKeyDown(Keys.F);

        if (!increase && !decrease)
        {
            return;
        }

        const float delta = 0.05f;
        var amount = increase ? delta : -delta;

        foreach (var entity in _world.Query<DroneMotionComponent>())
        {
            var motion = _world.GetComponent<DroneMotionComponent>(entity);
            motion.MoveDistance = MathHelper.Clamp(motion.MoveDistance + amount, 1.0f, 10.0f);
        }
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
}