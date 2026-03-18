using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database;

public class GameCache
{
    private readonly ConcurrentDictionary<DocumentReference, (Game game, NonDbGameState nonDbGameState)> _gamesByDocumentRef = new();
    private readonly ConcurrentDictionary<ulong, (Game game, NonDbGameState nonDbGameState)> _gamesByChannelId = new();

    public (Game game, NonDbGameState nonDbGameState)? GetGame(DocumentReference documentRef) => _gamesByDocumentRef.GetValueOrDefault(documentRef);
    public bool GetGame(DocumentReference documentRef, [NotNullWhen(true)] out Game? game, [NotNullWhen(true)] out NonDbGameState? nonDbGameState)
    {
        var tuple = _gamesByDocumentRef.GetValueOrDefault(documentRef);
        game = tuple.game;
        nonDbGameState = tuple.nonDbGameState;
        return game != null!;
    }

    public (Game game, NonDbGameState nonDbGameState)? GetGame(DiscordChannel channel)
    {
        if (channel is DiscordThreadChannel threadChannel)
        {
            channel = threadChannel.Parent;
        }
        return _gamesByChannelId.GetValueOrDefault(channel.Id);
    }

    public void AddOrUpdateGame(Game game, NonDbGameState nonDbGameState)
    {
        _gamesByDocumentRef[game.DocumentId!] = (game, nonDbGameState);
        _gamesByChannelId[game.GameChannelId] = (game, nonDbGameState);
    }

    public void Clear(Game game)
    {
        _gamesByDocumentRef.Remove(game.DocumentId!, out _);
        _gamesByChannelId.Remove(game.GameChannelId, out var tuple);
        
        tuple.nonDbGameState.TurnProdTimer.Cancel();
        tuple.nonDbGameState.UnfinishedTurnProdTimer.Cancel();
    }

    public void Clear(DocumentReference documentRef)
    {
        var tuple = GetGame(documentRef);
        if (tuple != null)
        {
            Clear(tuple.Value.game);
        }
    }
    
    public void ClearAll()
    {
        _gamesByDocumentRef.Clear();
        _gamesByChannelId.Clear();
    }
}