using Microsoft.Xna.Framework;
using TacticalImpact.MonoGame.Ecs.Components;

namespace TacticalImpact.MonoGame.Ecs.Systems;

public sealed class ExplosionUpdateSystem : ISystem
{
    public void Update(EcsWorld world, float deltaTimeSeconds)
    {
        var toDestroy = new List<int>();

        foreach (var entity in world.Query<ExplosionVisualComponent>())
        {
            var explosion = world.GetComponent<ExplosionVisualComponent>(entity);
            explosion.Age += deltaTimeSeconds;

            if (explosion.Age >= explosion.MaxAge)
            {
                toDestroy.Add(entity);
            }
            else
            {
                // Linearly interpolate the radius expansion
                var progress = explosion.Age / explosion.MaxAge;
                explosion.CurrentRadius = MathHelper.Lerp(0.1f, explosion.MaxRadius, progress);
            }
        }

        for (var i = 0; i < toDestroy.Count; i++)
        {
            world.DestroyEntity(toDestroy[i]);
        }
    }
}
