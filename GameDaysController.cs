namespace JailBreak.Games.GameDays;

public class GameDaysController : IFeature
{
    private GameDay? _activeGame;

    public void RegisterGame(GameDay gameDay)
    {
        if (!IsGame())
            _activeGame = gameDay;
    }

    public bool IsGame()
    {
        return _activeGame != null! && _activeGame.GameStarted;
    }
}