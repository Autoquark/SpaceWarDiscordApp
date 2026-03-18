using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.MapGeneration;

public class EuropeMapGenerator : BaseMapGenerator
{
    public EuropeMapGenerator() : base("europe", "Space Europe", [3, 4, 5, 6])
    {
        Description = "It's Europe! In space!";
    }

    private void SetPlanetOwnership(List<BoardHex> hexes, int playerId)
    {
        foreach (BoardHex hex in hexes)
        {
            int forces = 0;
            if (playerId != -1)
            {
                forces++;
                if (hex.Planet.IsHomeSystem)
                {
                    forces++;
                }
            }
            hex.Planet.SetForces(forces, playerId);
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
        BoardHex sea = new BoardHex() {  Planet = new Planet() };
        BoardHex neutralStar = new BoardHex() { Planet = new Planet() { Stars = 1, Production = 1, Science = 0, ForcesPresent = 0, } };
        BoardHex neutralScience = new BoardHex() { Planet = new Planet() { Stars = 0, Production = 1, Science = 1, ForcesPresent = 0, } };
        BoardHex neutralPeople = new BoardHex() { Planet = new Planet() { Stars = 0, Production = 3, Science = 0, ForcesPresent = 0, } };

        var map = new List<BoardHex>();

        // UK
        game.Players[0].PlayerColour = PlayerColour.Blue;
        SetPlanetOwnership(ownedBoardHexes, game.Players[0].GamePlayerId);
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(0, -4) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(0, -3) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(0, -2) });
        map.Add(new BoardHex() { Planet = new Planet() { Stars = 1, Production = 2, Science = 1, ForcesPresent = 0, }, Coordinates = new HexCoordinates(0, -1) });
        map.Add(new BoardHex(star) { Coordinates = new HexCoordinates(-1, -4) });
        map.Add(new BoardHex(science) { Coordinates = new HexCoordinates(-1, -3) });
        map.Add(new BoardHex(capital) { Coordinates = new HexCoordinates(-1, -2) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-1, -1) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-2, -3) });
        map.Add(new BoardHex(people) { Coordinates = new HexCoordinates(-2, -2) });
        map.Add(new BoardHex() { Coordinates = new HexCoordinates(-2, -4), HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthEast, HexDirection.South)] });
        map.Add(new BoardHex() { Coordinates = new HexCoordinates(-1, -5), HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthWest, HexDirection.SouthEast)] });
        map.Add(new BoardHex() { 
            Coordinates = new HexCoordinates(0, -5), 
            HyperlaneConnections = [
                new HyperlaneConnection(HexDirection.SouthWest, HexDirection.SouthEast), 
                new HyperlaneConnection(HexDirection.NorthWest, HexDirection.SouthEast)
                ]
        });
        map.Add(new BoardHex() { Coordinates = new HexCoordinates(-3, -2), HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthEast, HexDirection.South)] });
        map.Add(new BoardHex() { Coordinates = new HexCoordinates(-3, -1), HyperlaneConnections = [new HyperlaneConnection(HexDirection.North, HexDirection.South)] });
        map.Add(new BoardHex() { 
            Coordinates = new HexCoordinates(-3, 0), 
            HyperlaneConnections = [
                new HyperlaneConnection(HexDirection.North, HexDirection.South),
                new HyperlaneConnection(HexDirection.NorthEast, HexDirection.South)
                ]
        });
        map.Add(new BoardHex()
        {
            Coordinates = new HexCoordinates(-2, -1),
            HyperlaneConnections = [
                new HyperlaneConnection(HexDirection.North, HexDirection.SouthEast),
                new HyperlaneConnection(HexDirection.North, HexDirection.SouthWest)
                ]
        });

        // France
        game.Players[1].PlayerColour = PlayerColour.Cyan;
        SetPlanetOwnership(ownedBoardHexes, game.Players[1].GamePlayerId);
        map.Add(new BoardHex(capital) { Coordinates = new HexCoordinates(-1, 0) });
        map.Add(new BoardHex(star) { Coordinates = new HexCoordinates(-1, 1) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-1, 2) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-1, 3) });
        map.Add(new BoardHex(science) { Coordinates = new HexCoordinates(-2, 0) });
        map.Add(new BoardHex(people) { Coordinates = new HexCoordinates(-2, 1) });
        map.Add(new BoardHex(neutralPeople) { Coordinates = new HexCoordinates(-2, 2) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-2, 3) });
        map.Add(new BoardHex(neutralStar) { Coordinates = new HexCoordinates(-2, 4) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(-3, 1) });
        map.Add(new BoardHex(neutralStar) { Coordinates = new HexCoordinates(-3, 3) });
        map.Add(new BoardHex()
        {
            Coordinates = new HexCoordinates(-3, 2),
            HyperlaneConnections = [
                new HyperlaneConnection(HexDirection.North, HexDirection.SouthEast),
                new HyperlaneConnection(HexDirection.North, HexDirection.South),
                new HyperlaneConnection(HexDirection.North, HexDirection.SouthWest),
                ]
        });
        map.Add(new BoardHex() { Coordinates = new HexCoordinates(-4, 3), HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthEast, HexDirection.South)] });
        map.Add(new BoardHex() { Coordinates = new HexCoordinates(-4, 4), HyperlaneConnections = [new HyperlaneConnection(HexDirection.North, HexDirection.SouthEast)] });
        map.Add(new BoardHex() { Coordinates = new HexCoordinates(-3, 4), HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthWest, HexDirection.NorthEast)] });

        // Russia
        if (playerCount > 2)
        {
            game.Players[2].PlayerColour = PlayerColour.Yellow;
        }
        SetPlanetOwnership(ownedBoardHexes, game.Players.ElementAtOrDefault(2)?.GamePlayerId ?? -1);
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(1, -5) });
        map.Add(new BoardHex(neutralStar) { Coordinates = new HexCoordinates(1, -4) });
        map.Add(new BoardHex(neutralStar) { Coordinates = new HexCoordinates(2, -5) });
        map.Add(new BoardHex(neutralScience) { Coordinates = new HexCoordinates(2, -4) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(2, -3) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(3, -6) });
        map.Add(new BoardHex(star) { Coordinates = new HexCoordinates(3, -5) });
        map.Add(new BoardHex(capital) { Coordinates = new HexCoordinates(3, -4) });
        map.Add(new BoardHex(science) { Coordinates = new HexCoordinates(3, -3) });
        map.Add(new BoardHex(people) { Coordinates = new HexCoordinates(3, -2) });
        map.Add(new BoardHex() { Coordinates = new HexCoordinates(2, -6), HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthWest, HexDirection.SouthEast)] });
        map.Add(new BoardHex() 
        { 
            Coordinates = new HexCoordinates(1, -3), 
            HyperlaneConnections = [
            new HyperlaneConnection(HexDirection.NorthWest, HexDirection.SouthEast),
            new HyperlaneConnection(HexDirection.North, HexDirection.South)
            ]
        });

        // Austria-Hungary
        if (playerCount > 3)
        {
            game.Players[3].PlayerColour = PlayerColour.Red;
        }
        SetPlanetOwnership(ownedBoardHexes, game.Players.ElementAtOrDefault(3)?.GamePlayerId ?? -1);
        map.Add(new BoardHex(star) { Coordinates = new HexCoordinates(1, 0) });
        map.Add(new BoardHex(people) { Coordinates = new HexCoordinates(2, -1) });
        map.Add(new BoardHex(capital) { Coordinates = new HexCoordinates(2, 0) });
        map.Add(new BoardHex(science) { Coordinates = new HexCoordinates(3, -1) });
        map.Add(new BoardHex(neutralStar) { Coordinates = new HexCoordinates(3, 0) });

        // Germany
        if (playerCount > 4)
        {
            game.Players[4].PlayerColour = PlayerColour.Orange;
        }
        SetPlanetOwnership(ownedBoardHexes, game.Players.ElementAtOrDefault(4)?.GamePlayerId ?? -1);
        map.Add(new BoardHex(science) { Coordinates = new HexCoordinates(0, 0) });
        map.Add(new BoardHex(star) { Coordinates = new HexCoordinates(1, -2) });
        map.Add(new BoardHex(people) { Coordinates = new HexCoordinates(1, -1) });
        map.Add(new BoardHex(capital) { Coordinates = new HexCoordinates(2, -2) });

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
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(2, 1) });
        map.Add(new BoardHex(sea) { Coordinates = new HexCoordinates(1, 3) });
        map.Add(new BoardHex()
        {
            Coordinates = new HexCoordinates(0, 2),
            HyperlaneConnections = [
                new HyperlaneConnection(HexDirection.North, HexDirection.SouthWest),
                new HyperlaneConnection(HexDirection.NorthEast, HexDirection.SouthWest),
                new HyperlaneConnection(HexDirection.SouthEast, HexDirection.SouthWest),
                ]
        });
        map.Add(new BoardHex() { Coordinates = new HexCoordinates(-1, 4), HyperlaneConnections = [new HyperlaneConnection(HexDirection.North, HexDirection.SouthEast)] });
        map.Add(new BoardHex() { Coordinates = new HexCoordinates(0, 4), HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthWest, HexDirection.NorthEast)] });
        map.Add(new BoardHex() { Coordinates = new HexCoordinates(2, 2), HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthWest, HexDirection.North)] });

        return map;
    }
}