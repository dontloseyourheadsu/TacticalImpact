using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace TacticalImpact.MonoGame.Rendering;

public sealed class OrbitCamera3D
{
    private float _yaw = -0.85f;
    private float _pitch = -0.35f;
    private float _distance = 22f;
    private int _previousWheelValue;
    private MouseState _previousMouseState;
    private TouchCollection _previousTouchState;

    public Vector3 Target { get; set; } = new Vector3(0f, 1.5f, 0f);
    public Matrix View { get; private set; } = Matrix.Identity;
    public Matrix Projection { get; private set; } = Matrix.Identity;

    public void Update(GameTime gameTime, Viewport viewport)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var touches = TouchPanel.GetState();

        var rotationSpeed = 1.9f;
        if (keyboard.IsKeyDown(Keys.Left))
        {
            _yaw -= rotationSpeed * dt;
        }

        if (keyboard.IsKeyDown(Keys.Right))
        {
            _yaw += rotationSpeed * dt;
        }

        if (keyboard.IsKeyDown(Keys.Up))
        {
            _pitch -= rotationSpeed * dt;
        }

        if (keyboard.IsKeyDown(Keys.Down))
        {
            _pitch += rotationSpeed * dt;
        }

        if (mouse.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Pressed)
        {
            var deltaX = mouse.X - _previousMouseState.X;
            var deltaY = mouse.Y - _previousMouseState.Y;
            _yaw += deltaX * 0.01f;
            _pitch += deltaY * 0.01f;
        }

        var wheelDelta = mouse.ScrollWheelValue - _previousWheelValue;
        _distance -= wheelDelta * 0.008f;

        if (keyboard.IsKeyDown(Keys.Q))
        {
            _distance += 12f * dt;
        }

        if (keyboard.IsKeyDown(Keys.E))
        {
            _distance -= 12f * dt;
        }

        if (touches.Count == 1 && _previousTouchState.Count == 1)
        {
            var delta = touches[0].Position - _previousTouchState[0].Position;
            _yaw -= delta.X * 0.008f;
            _pitch += delta.Y * 0.008f;
        }

        if (touches.Count == 2 && _previousTouchState.Count == 2)
        {
            var previousDistance = Vector2.Distance(_previousTouchState[0].Position, _previousTouchState[1].Position);
            var currentDistance = Vector2.Distance(touches[0].Position, touches[1].Position);
            var pinchDelta = currentDistance - previousDistance;
            _distance -= pinchDelta * 0.015f;
        }

        _pitch = MathHelper.Clamp(_pitch, -1.25f, 0.3f);
        _distance = MathHelper.Clamp(_distance, 6f, 60f);

        var lookDirection = new Vector3(
            MathF.Cos(_pitch) * MathF.Cos(_yaw),
            MathF.Sin(_pitch),
            MathF.Cos(_pitch) * MathF.Sin(_yaw));

        var cameraPosition = Target + (lookDirection * _distance);

        View = Matrix.CreateLookAt(cameraPosition, Target, Vector3.Up);
        Projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(60f),
            viewport.AspectRatio,
            0.1f,
            500f);

        _previousMouseState = mouse;
        _previousWheelValue = mouse.ScrollWheelValue;
        _previousTouchState = touches;
    }
}