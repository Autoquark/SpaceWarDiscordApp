using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

public class GameCache
{
    private readonly Dictionary<DocumentReference, Game> _gamesByDocumentRef = new();
    private readonly Dictionary<ulong, Game> _gamesByChannelId = new();
    
    public Game? GetGame(DocumentReference documentRef) => _gamesByDocumentRef.GetValueOrDefault(documentRef);
    public Game? GetGame(ulong channelId) => _gamesByChannelId.GetValueOrDefault(channelId);

    public void AddOrUpdateGame(Game game)
    {
        _gamesByDocumentRef[game.DocumentId!] = game;
        _gamesByChannelId[game.GameChannelId] = game;
    }

    public void Clear(Game game)
    {
        _gamesByDocumentRef.Remove(game.DocumentId!);
        _gamesByChannelId.Remove(game.GameChannelId);
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