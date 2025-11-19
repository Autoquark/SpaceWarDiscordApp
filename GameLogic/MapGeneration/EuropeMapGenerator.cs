using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.MapGeneration;

public class EuropeMapGenerator : BaseMapGenerator
{
    public EuropeMapGenerator() : base("europe", "Space Europe", [2, 3, 4, 5, 6])
    {
    }

    private void SetPlanetOwnership(List<BoardHex> hexes, int playerId)
    {
        foreach (BoardHex hex in hexes)
        {
            hex.Planet.OwningPlayerId = playerId;
        }
    }

    public override List<BoardHex> GenerateMapInternal(Game game)
    {
        var playerCount = game.Players.Count;

        // Default tiles
        BoardHex capital = new BoardHex() { Planet = new Planet() { Stars = 2, Production = 2, Science = 1, IsHomeSystem = true, ForcesPresent = 2, } };
        BoardHex science = new BoardHex() { Planet = new Planet() { Stars = 0, Production = 1, Science = 1, ForcesPresent = 1, } };
        BoardHex people = new BoardHex() { Planet = new Planet() { Stars = 0, Production = 3, Science = 0, ForcesPresent = 1, } };
        BoardHex star = new BoardHex() { Planet = new Planet() { Stars = 1, Production = 1, Science = 0, ForcesPresent = 1, } };
        List<BoardHex> ownedBoardHexes = new List<BoardHex>() { capital, science, people, star };
        BoardHex sea = new BoardHex();
        BoardHex neutral = new BoardHex() { Planet = new Planet() { Stars = 1, Production = 1, Science = 0, ForcesPresent = 0, } };

        var map = new List<BoardHex>();

        // UK
        game.Players[0].PlayerColour = PlayerColour.Blue;
        SetPlanetOwnership(ownedBoardHexes, game.Players[0].GamePlayerId);
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(0, -4) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(0, -3) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(0, -2) });
        map.Add(new BoardHex(neutral) { Coordinates = new HexCoordinates(0, -1) });
        map.Add(new BoardHex(star) { Coordinates = new HexCoordinates(-1, -4) });
        map.Add(new BoardHex(science) { Coordinates = new HexCoordinates(-1, -3) });
        map.Add(new BoardHex(capital) { Coordinates = new HexCoordinates(-1, -2) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-1, -1) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-2, -3) });
        map.Add(new BoardHex(people) { Coordinates = new HexCoordinates(-2, -2) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-2, -1) });

        // France
        game.Players[1].PlayerColour = PlayerColour.Cyan;
        SetPlanetOwnership(ownedBoardHexes, game.Players[1].GamePlayerId);
        map.Add(new BoardHex(capital) { Coordinates = new HexCoordinates(-1, 0) });
        map.Add(new BoardHex(star) { Coordinates = new HexCoordinates(-1, 1) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-1, 2) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-1, 3) });
        map.Add(new BoardHex(science) { Coordinates = new HexCoordinates(-2, 0) });
        map.Add(new BoardHex(people) { Coordinates = new HexCoordinates(-2, 1) });
        map.Add(new BoardHex(neutral) { Coordinates = new HexCoordinates(-2, 2) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-2, 3) });
        map.Add(new BoardHex(neutral) { Coordinates = new HexCoordinates(-2, 4) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-3, 1) });
        map.Add(new BoardHex(neutral) { Coordinates = new HexCoordinates(-3, 3) });

        // Germany
        if (playerCount > 2)
        {
            game.Players[2].PlayerColour = PlayerColour.Orange;
        }
        SetPlanetOwnership(ownedBoardHexes, game.Players.ElementAtOrDefault(2)?.GamePlayerId ?? -1);
        map.Add(new BoardHex(science) { Coordinates = new HexCoordinates(0, 0) });
        map.Add(new BoardHex(star) { Coordinates = new HexCoordinates(1, -2) });
        map.Add(new BoardHex(people) { Coordinates = new HexCoordinates(1, -1) });
        map.Add(new BoardHex(capital) { Coordinates = new HexCoordinates(2, -2) });

        // Russia
        if (playerCount > 3)
        {
            game.Players[3].PlayerColour = PlayerColour.Yellow;
        }
        SetPlanetOwnership(ownedBoardHexes, game.Players.ElementAtOrDefault(3)?.GamePlayerId ?? -1);
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(1, -5) });
        map.Add(new BoardHex(neutral) { Coordinates = new HexCoordinates(1, -4) });
        map.Add(new BoardHex(neutral) { Coordinates = new HexCoordinates(2, -5) });
        map.Add(new BoardHex(neutral) { Coordinates = new HexCoordinates(2, -4) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(2, -3) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(3, -6) });
        map.Add(new BoardHex(star) { Coordinates = new HexCoordinates(3, -5) });
        map.Add(new BoardHex(capital) { Coordinates = new HexCoordinates(3, -4) });
        map.Add(new BoardHex(science) { Coordinates = new HexCoordinates(3, -3) });
        map.Add(new BoardHex(people) { Coordinates = new HexCoordinates(3, -2) });

        // Austria-Hungary
        if (playerCount > 4)
        {
            game.Players[4].PlayerColour = PlayerColour.Red;
        }
        SetPlanetOwnership(ownedBoardHexes, game.Players.ElementAtOrDefault(4)?.GamePlayerId ?? -1);
        map.Add(new BoardHex(star) { Coordinates = new HexCoordinates(1, 0) });
        map.Add(new BoardHex(people) { Coordinates = new HexCoordinates(2, -1) });
        map.Add(new BoardHex(capital) { Coordinates = new HexCoordinates(2, 0) });
        map.Add(new BoardHex(science) { Coordinates = new HexCoordinates(3, -1) });
        map.Add(new BoardHex(neutral) { Coordinates = new HexCoordinates(3, 0) });

        // Italy
        if (playerCount > 5)
        {
            game.Players[5].PlayerColour = PlayerColour.Green;
        }
        SetPlanetOwnership(ownedBoardHexes, game.Players.ElementAtOrDefault(5)?.GamePlayerId ?? -1);
        map.Add(new BoardHex(people) { Coordinates = new HexCoordinates(0, 1) });
        map.Add(new BoardHex(science) { Coordinates = new HexCoordinates(1, 1) });
        map.Add(new BoardHex(capital) { Coordinates = new HexCoordinates(1, 2) });
        map.Add(new BoardHex(star) { Coordinates = new HexCoordinates(0, 3) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(1, 3) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(2, 1) });


        return map;
    }
}