using CounterStrikeSharp.API.Core;
using JailBreak.Games.LRGames;
using Microsoft.Extensions.DependencyInjection;

namespace JailBreak.LRGames;

public class LRGamesFactory : IFeature
{
    private readonly LRGameController _lrGameController;
    private readonly IServiceProvider _serviceProvider;

    public LRGamesFactory(LRGameController lrGameController, IServiceProvider serviceProvider)
    {
        _lrGameController = lrGameController;
        _serviceProvider = serviceProvider;
    }

    public LRGame Create(CCSPlayerController player, Type gameType)
    {
        var game = (LRGame)_serviceProvider.GetRequiredService(gameType);
        _lrGameController.RegisterGame(game, player);

        return game;
    }
}