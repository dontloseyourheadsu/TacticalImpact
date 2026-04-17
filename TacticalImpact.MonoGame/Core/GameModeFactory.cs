using TacticalImpact.MonoGame.Modes;

namespace TacticalImpact.MonoGame.Core;

public sealed class GameModeFactory
{
    public IGameMode Create(GameMode mode, TacticalImpactGame game)
    {
        return mode switch
        {
            GameMode.Playground => new PlaygroundMode(game),
            GameMode.Normal => new NormalMode(game),
            _ => new PlaygroundMode(game)
        };
    }
}