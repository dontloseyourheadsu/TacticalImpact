namespace TacticalImpact.MonoGame.Ecs.Systems;

public interface ISystem
{
    void Update(EcsWorld world, float deltaTimeSeconds);
}