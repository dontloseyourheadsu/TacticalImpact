using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TacticalImpact.MonoGame.Modes;

public sealed class NormalMode : IGameMode
{
    private bool _loaded;

    public NormalMode(TacticalImpactGame game)
    {
    }

    public void Initialize()
    {
    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _loaded = true;
    }

    public void Update(GameTime gameTime)
    {
    }

    public void Draw(GameTime gameTime, GraphicsDevice graphicsDevice)
    {
        if (!_loaded)
        {
            return;
        }

        graphicsDevice.Clear(new Color(24, 22, 30));
    }

    public void Dispose()
    {
    }
}