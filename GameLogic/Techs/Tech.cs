using System.Collections.ObjectModel;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public abstract class Tech
{
    static Tech()
    {
        TechsById = new ReadOnlyDictionary<string, Tech>(_techsById);
    }
    
    public static IReadOnlyDictionary<string, Tech> TechsById;

    private static readonly Dictionary<string, Tech> _techsById = new();
    
    protected Tech(string id, string displayName, string description, string flavourText)
    {
        Id = id;
        Description = description;
        FlavourText = flavourText;
        DisplayName = displayName;
        if (!_techsById.TryAdd(id, this))
        {
            throw new ArgumentException($"Tech {id} already exists");
        }
    }
    
    /// <summary>
    /// Unique ID for this tech. Changing this will break games in progress.
    /// </summary>
    public string Id { get; }
    
    public string DisplayName { get; }
    
    /// <summary>
    /// Player facing rules text for this tech
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Player facing flavour text for this tech
    /// </summary>
    public string FlavourText { get; }

    /// <summary>
    /// If true, the default GetActions implementation will return a single action with properties based on this tech's
    /// properties
    /// </summary>
    protected bool HasSimpleAction { get; init; } = false;

    protected ActionType SimpleActionType { get; init; } = ActionType.Main;

    /// <summary>
    /// Gets a discord message string representing the state of this tech for the given game and player
    /// </summary>
    public virtual string GetTechDisplayString(Game game, GamePlayer player)
    {
        var result = DisplayName;
        if (player.GetPlayerTechById(Id).IsExhausted)
        {
            result = result.DiscordStrikeThrough(); //TODO: Emoji
        }
        
        return result;
    }

    /// <summary>
    /// Get all actions associated with this tech
    /// </summary>
    public virtual IEnumerable<TechAction> GetActions(Game game, GamePlayer player) =>
        HasSimpleAction ? [new TechAction(this, SimpleActionType) { DisplayName = DisplayName, ActionType = SimpleActionType, IsAvailable = !player.GetPlayerTechById(Id).IsExhausted && (!game.ActionTakenThisTurn || SimpleActionType == ActionType.Free) }] : [];

    public virtual Task<TBuilder> UseTechActionAsync<TBuilder>(
        TBuilder builder, Game game, GamePlayer player)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder> => Task.FromResult(builder);

    /// <summary>
    /// Create a PlayerTech for a player that has just acquired this tech, to track related game state
    /// </summary>
    public virtual PlayerTech CreatePlayerTech(Game game, GamePlayer player) => new()
        {
            TechId = Id
        };
}