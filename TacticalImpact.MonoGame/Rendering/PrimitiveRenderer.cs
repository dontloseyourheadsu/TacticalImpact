using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TacticalImpact.MonoGame.Rendering;

public sealed class PrimitiveRenderer : IDisposable
{
    private Texture2D? _pixel;

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    public void FillRectangle(SpriteBatch spriteBatch, Rectangle rectangle, Color color)
    {
        if (_pixel is null)
        {
            return;
        }

        spriteBatch.Draw(_pixel, rectangle, color);
    }

    public void DrawCheckerboard(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        int cellSize,
        Color colorA,
        Color colorB)
    {
        if (_pixel is null || cellSize <= 0)
        {
            return;
        }

        for (var y = bounds.Top; y < bounds.Bottom; y += cellSize)
        {
            for (var x = bounds.Left; x < bounds.Right; x += cellSize)
            {
                var xx = x - bounds.Left;
                var yy = y - bounds.Top;
                var checker = ((xx / cellSize) + (yy / cellSize)) % 2 == 0;
                var tileColor = checker ? colorA : colorB;

                var width = Math.Min(cellSize, bounds.Right - x);
                var height = Math.Min(cellSize, bounds.Bottom - y);
                spriteBatch.Draw(_pixel, new Rectangle(x, y, width, height), tileColor);
            }
        }
    }

    public void Dispose()
    {
        _pixel?.Dispose();
        _pixel = null;
    }
}