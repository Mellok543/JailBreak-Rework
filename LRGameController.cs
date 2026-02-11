using CounterStrikeSharp.API.Core;

namespace JailBreak.Games.LRGames;

public class LRGameController : IFeature
{
    private Dictionary<CCSPlayerController, LRGame> _LrGames { get; set; } = new();

    public LRGameController(JailBreak _jailBreak)
    {
        _jailBreak.RegisterEventHandler<EventRoundStart>(((@event, info) =>
        {
            _LrGames.Clear();
            return HookResult.Continue;
        }));
    }

    public bool PlayerAlreadyPlay(CCSPlayerController player)
    {
        if (_LrGames.TryGetValue(player, out var game) && game.GameState == GameState.Process)
        {
            return true;
        }

        return false;
    }

    public void RegisterGame(LRGame lrGame, CCSPlayerController player)
    {
        if (_LrGames.TryGetValue(player, out var game))
        {
            game.InternalCancel();
        }

        _LrGames[player] = lrGame;
        lrGame.OnGameEnd += GameEnd;
    }

    private void GameEnd(LRGame lrGame)
    {
        _LrGames.Remove(lrGame.Guardian);
        _LrGames.Remove(lrGame.Inmate);

        lrGame.OnGameEnd -= GameEnd;
    }

    public void RegisterGuardian(LRGame lrGame, CCSPlayerController player)
    {
        _LrGames[player] = lrGame;
    }
}