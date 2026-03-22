using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;

namespace Tumult.Database;

public class GameCache<TGame, TNonDbState>
    where TGame : BaseGame
    where TNonDbState : IDisposable
{
    private readonly ConcurrentDictionary<DocumentReference, (TGame game, TNonDbState nonDbGameState)> _gamesByDocumentRef = new();
    private readonly ConcurrentDictionary<ulong, (TGame game, TNonDbState nonDbGameState)> _gamesByChannelId = new();

    public (TGame game, TNonDbState nonDbGameState)? GetGame(DocumentReference documentRef) => _gamesByDocumentRef.GetValueOrDefault(documentRef);

    public bool GetGame(DocumentReference documentRef, [NotNullWhen(true)] out TGame? game, [NotNullWhen(true)] out TNonDbState? nonDbGameState)
    {
        var tuple = _gamesByDocumentRef.GetValueOrDefault(documentRef);
        game = tuple.game;
        nonDbGameState = tuple.nonDbGameState;
        return game != null!;
    }

    public (TGame game, TNonDbState nonDbGameState)? GetGame(DiscordChannel channel)
    {
        if (channel is DiscordThreadChannel threadChannel)
        {
            channel = threadChannel.Parent;
        }
        return _gamesByChannelId.GetValueOrDefault(channel.Id);
    }

    public void AddOrUpdateGame(TGame game, TNonDbState nonDbGameState)
    {
        _gamesByDocumentRef[game.DocumentId!] = (game, nonDbGameState);
        _gamesByChannelId[game.GameChannelId] = (game, nonDbGameState);
    }

    public void Clear(TGame game)
    {
        _gamesByDocumentRef.Remove(game.DocumentId!, out _);
        _gamesByChannelId.Remove(game.GameChannelId, out var tuple);
        tuple.nonDbGameState?.Dispose();
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
