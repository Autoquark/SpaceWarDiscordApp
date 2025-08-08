using System.Collections.ObjectModel;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.GameEvents;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public abstract class Tech
{
    static Tech()
    {
        TechsById = new ReadOnlyDictionary<string, Tech>(_techsById);
    }

    /// <summary>
    /// Additional objects that need to be registered as interaction and/or event resolved handlers
    /// </summary>
    public IEnumerable<object> AdditionalHandlers { get; init; } = [];
    
    public static readonly IReadOnlyDictionary<string, Tech> TechsById;

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

    public ActionType SimpleActionType { get; protected init; } = ActionType.Main;
    
    protected bool SimpleActionIsOncePerTurn { get; init; } = false;

    /// <summary>
    /// Gets a discord message string representing the state of this tech for the given game and player
    /// </summary>
    public virtual string GetTechDisplayString(Game game, GamePlayer player)
    {
        var result = DisplayName;
        var tech = player.GetPlayerTechById(Id);
        if (tech.IsExhausted || (HasSimpleAction && SimpleActionIsOncePerTurn && tech.UsedThisTurn))
        {
            result = result.DiscordStrikeThrough(); //TODO: Emoji
        }
        
        return result;
    }

    /// <summary>
    /// Get triggered effects from this tech in response to the given GameEvent
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerable<TriggeredEffect> GetTriggeredEffects(Game game, GameEvent gameEvent, GamePlayer player)
        => GetThisTech(player).IsExhausted ? [] : GetTriggeredEffectsInternal(game, gameEvent, player);
    
    protected virtual IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player) => [];

    /// <summary>
    /// Get all actions associated with this tech
    /// </summary>
    public virtual IEnumerable<TechAction> GetActions(Game game, GamePlayer player) =>
        HasSimpleAction ? [new TechAction(this, SimpleActionType)
            {
                DisplayName = DisplayName,
                ActionType = SimpleActionType,
                IsAvailable = IsSimpleActionAvailable(game, player)
            }]
            : [];

    public virtual Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider) => Task.FromResult(builder);

    /// <summary>
    /// Create a PlayerTech for a player that has just acquired this tech, to track related game state
    /// </summary>
    public virtual PlayerTech CreatePlayerTech(Game game, GamePlayer player) => new()
        {
            TechId = Id,
            Game = game.DocumentId!
        };
    
    protected virtual bool IsSimpleActionAvailable(Game game, GamePlayer player)
    {
        var tech = player.GetPlayerTechById(Id);
        return !tech.IsExhausted
               && (!game.ActionTakenThisTurn || SimpleActionType == ActionType.Free)
               && !(SimpleActionIsOncePerTurn && tech.UsedThisTurn);
    }
    
    protected PlayerTech GetThisTech(GamePlayer player) => player.GetPlayerTechById<PlayerTech>(Id);
    protected T GetThisTech<T>(GamePlayer player) where T : PlayerTech => player.GetPlayerTechById<T>(Id);
}