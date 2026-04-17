using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TacticalImpact.MonoGame.Modes;

public interface IGameMode : IDisposable
{
    void Initialize();
    void LoadContent(ContentManager content, GraphicsDevice graphicsDevice);
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime, GraphicsDevice graphicsDevice);
}