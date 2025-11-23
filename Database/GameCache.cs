using System.Collections.Concurrent;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

public class GameCache
{
    private readonly ConcurrentDictionary<DocumentReference, Game> _gamesByDocumentRef = new();
    private readonly ConcurrentDictionary<ulong, Game> _gamesByChannelId = new();

    public Game? GetGame(DocumentReference documentRef) => _gamesByDocumentRef.GetValueOrDefault(documentRef);
    public Game? GetGame(DiscordChannel channel)
    {
        if (channel is DiscordThreadChannel threadChannel)
        {
            channel = threadChannel.Parent;
        }
        return _gamesByChannelId.GetValueOrDefault(channel.Id);
    }

    public void AddOrUpdateGame(Game game)
    {
        _gamesByDocumentRef[game.DocumentId!] = game;
        _gamesByChannelId[game.GameChannelId] = game;
    }

    public void Clear(Game game)
    {
        _gamesByDocumentRef.Remove(game.DocumentId!, out _);
        _gamesByChannelId.Remove(game.GameChannelId, out _);
    }

    public void Clear(DocumentReference documentRef)
    {
        var game = GetGame(documentRef);
        if (game != null)
        {
            Clear(game);
        }
    }
    
    public void ClearAll()
    {
        _gamesByDocumentRef.Clear();
        _gamesByChannelId.Clear();
    }
}