using Microsoft.Xna.Framework;

namespace TacticalImpact.MonoGame.Ecs.Components;

public sealed class PackageRenderComponent
{
    public Vector3 Size { get; set; } = new Vector3(0.58f, 0.42f, 0.58f);
    public Color BaseColor { get; set; } = new Color(222, 154, 79);
}
