using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using TacticalImpact.MonoGame.Core;
using TacticalImpact.MonoGame.Modes;

namespace TacticalImpact.MonoGame;

public sealed class TacticalImpactGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly GameModeFactory _modeFactory;

    private IGameMode? _activeMode;
    private GameMode _activeModeType;

    private KeyboardState _previousKeyboard;
    private bool _touchWasActive;

    public TacticalImpactGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _modeFactory = new GameModeFactory();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;

        TouchPanel.EnabledGestures = GestureType.Tap;
        TouchPanel.DisplayWidth = _graphics.PreferredBackBufferWidth;
        TouchPanel.DisplayHeight = _graphics.PreferredBackBufferHeight;

        _activeModeType = ParseModeFromArgs(Environment.GetCommandLineArgs());
    }

    protected override void Initialize()
    {
        _activeMode = _modeFactory.Create(_activeModeType, this);
        _activeMode.Initialize();
        UpdateWindowTitle();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _activeMode?.LoadContent(Content, GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
            return;
        }

        HandleModeSwitchInput(keyboard);

        _activeMode?.Update(gameTime);
        _previousKeyboard = keyboard;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _activeMode?.Draw(gameTime, GraphicsDevice);
        base.Draw(gameTime);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        _activeMode?.Dispose();
        base.OnExiting(sender, args);
    }

    private void HandleModeSwitchInput(KeyboardState keyboard)
    {
        if (PressedNow(keyboard, Keys.F1))
        {
            SwitchMode(GameMode.Playground);
        }
        else if (PressedNow(keyboard, Keys.F2))
        {
            SwitchMode(GameMode.Normal);
        }

        var touchCollection = TouchPanel.GetState();
        var touchActive = touchCollection.Count > 0;
        if (touchActive && !_touchWasActive)
        {
            var touch = touchCollection[0];
            if (touch.Position.Y <= GraphicsDevice.Viewport.Height * 0.2f)
            {
                if (touch.Position.X < GraphicsDevice.Viewport.Width * 0.5f)
                {
                    SwitchMode(GameMode.Playground);
                }
                else
                {
                    SwitchMode(GameMode.Normal);
                }
            }
        }

        _touchWasActive = touchActive;
    }

    private bool PressedNow(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    }

    private void SwitchMode(GameMode mode)
    {
        if (_activeModeType == mode)
        {
            return;
        }

        _activeMode?.Dispose();
        _activeModeType = mode;
        _activeMode = _modeFactory.Create(_activeModeType, this);
        _activeMode.Initialize();

        _activeMode.LoadContent(Content, GraphicsDevice);

        UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        Window.Title = _activeModeType == GameMode.Playground
            ? "TacticalImpact - Playground (F1) / Normal (F2)"
            : "TacticalImpact - Normal (F2) / Playground (F1)";
    }

    private static GameMode ParseModeFromArgs(string[] args)
    {
        foreach (var arg in args)
        {
            if (!arg.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = arg.Substring("--mode=".Length).Trim().ToLowerInvariant();
            if (value == "normal")
            {
                return GameMode.Normal;
            }

            if (value == "playground")
            {
                return GameMode.Playground;
            }
        }

        return GameMode.Playground;
    }
}