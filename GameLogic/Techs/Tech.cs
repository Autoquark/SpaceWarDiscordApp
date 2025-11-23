using System.Collections.ObjectModel;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.GameEvents;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public enum TechKeyword
{
    Action,
    FreeAction,
    OncePerTurn,
    Exhaust,
    SingleUse
}

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
    
    protected Tech(string id, string displayName, string description, string flavourText, IEnumerable<TechKeyword>? descriptionKeywords = null)
    {
        Id = id;
        Description = description;
        FlavourText = flavourText;
        DisplayName = displayName;
        DescriptionKeywords = descriptionKeywords?.ToList() ?? [];
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

    public IReadOnlyList<TechKeyword> DescriptionKeywords { get; }
    
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
    public virtual IEnumerable<FormattedTextRun> GetTechDisplayString(Game game, GamePlayer player)
    {
        var result = new FormattedTextRun(DisplayName);
        var tech = player.GetPlayerTechById(Id);
        result.IsStrikethrough = tech.IsExhausted || (HasSimpleAction && SimpleActionIsOncePerTurn && tech.UsedThisTurn);
        
        return [result];
    }
    
    /// <summary>
    /// Called for each planet controlled by each player owning this tech to get any combat strength bonus that
    /// should be displayed on the map for the planet. 
    /// </summary>
    public virtual int GetDisplayedCombatStrengthBonus(Game game, BoardHex hex, GamePlayer player) => 0;
    
    /// <summary>
    /// Called for each planet controlled by each player owning this tech to get any production value bonus that
    /// should be displayed on the map for the planet. 
    /// </summary>
    public virtual int GetDisplayedProductionBonus(Game game, BoardHex hex, GamePlayer player) => 0;

    public virtual string GetTechStatusLine(Game game, GamePlayer player)
    {
        // If tech is limited use, show whether it is available
        if (DescriptionKeywords.Intersect([TechKeyword.Exhaust, TechKeyword.OncePerTurn]).Any())
        {
            var playerTech = player.GetPlayerTechById(Id);
            return playerTech switch
            {
                { IsExhausted: true } => "Exhausted",
                { UsedThisTurn: true } => "Used",
                _ => "Ready"
            };
        }

        // If tech is entirely passive, status line is blank
        return "";
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