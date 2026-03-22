# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This solution has two projects:

- **SpaceWarDiscordApp**: A Discord bot for asynchronous play of a turn-based board game. Players interact through Discord slash commands and button interactions, with game state persisted to Google Cloud Firestore.
- **Tumult**: A reusable framework library for Discord-based asynchronous games. SpaceWar depends on Tumult. Game-agnostic infrastructure (dispatchers, base classes, interfaces) lives here.

Game rules summary: https://docs.google.com/document/d/1IIoz7YV6zcvvbiPRysIUc0CdXpWRXhz_eLy-mYZpRak/

## Build and Run Commands

**Build the project:**
```bash
dotnet build SpaceWarDiscordApp.sln
```

**Run the bot:**
```bash
dotnet run --project SpaceWarDiscordApp.csproj
```

**Build specific files (for testing compilation):**
Use JetBrains IDE build tools or:
```bash
dotnet build SpaceWarDiscordApp.csproj
```

## Core Architecture

### Tumult Framework vs SpaceWar Game Code

The solution is split so that reusable Discord game infrastructure lives in **Tumult** and SpaceWar-specific logic stays in **SpaceWarDiscordApp**.

**Tumult owns:**
- Polymorphic Firestore base classes (`FirestoreDocument`, `PolymorphicFirestoreDocument`, `TypeDiscriminator`, etc.)
- `InteractionData` abstract base class and framework-level subclasses (`TriggeredEffectInteractionData`, `EventModifyingInteractionData`)
- `GameEvent` and `GameEvent_PlayerChoice<TInteractionData>` base classes
- Handler interfaces: `IInteractionHandler<T>`, `IPlayerChoiceEventHandler<TEvent, TInteractionData>`, `IEventResolvedHandler<T>`
- Dispatchers: `InteractionDispatcher` and `GameEventDispatcher` (generic over `TGame : BaseGame`)
- `BaseGame` abstract class — the minimal contract dispatchers need from any game
- `GameCache<TGame, TNonDbState>` — generic game cache; `TNonDbState : IDisposable` is disposed on eviction
- `GameSyncManager` — per-game async locking via `AsyncKeyedLocker<DocumentReference>`
- `IGameLoader<TGame>` — thin interface for loading and saving game state from/to Firestore (SpaceWar provides the concrete implementation; locking is handled separately by `GameSyncManager`)
- `InteractionOutcome`, `DiscordMultiMessageBuilder`, `ServiceProviderExtensions`

**SpaceWar owns:**
- `Game`, `GamePlayer`, `GameRules`, all board models
- All concrete `InteractionData` subclasses (~98 SpaceWar-specific interaction types)
- All concrete `GameEvent` subclasses
- All concrete handler implementations (commands, operations, techs)
- `GameCache`, `GameSyncManager`, `SpaceWarGameLoader`
- Image generation, AI features, SpaceWar-specific Discord message builders

### Event-Driven Game System

The game uses an event stack model where game actions push events onto `Game.EventStack`. Events are processed sequentially:

- **GameEvent** (Tumult): Base class for all game events
- **GameEvent_PlayerChoice<TInteractionData>** (Tumult): Events requiring player input via Discord interactions
- **GameEventDispatcher** (Tumult): Routes events to registered handlers implementing:
  - `IPlayerChoiceEventHandler<TEvent, TInteractionData>`: Handles player choice events
  - `IEventResolvedHandler<TEvent>`: Handles event resolution after choices are made

### Discord Interaction System

Discord interactions (button clicks, slash commands) are handled through:

- **InteractionDispatcher** (Tumult): Routes Discord interactions to registered handlers
- **IInteractionHandler<TInteractionData>** (Tumult): Interface for handling specific interaction types
- **InteractionData** (Tumult base / SpaceWar subclasses): Database models stored in Firestore with GUIDs used as Discord customId values

All handlers (Commands, Operations, Techs) are registered via `RegisterEverything()` in Program.cs, which registers them with both InteractionDispatcher and GameEventDispatcher.

### Database Architecture

Uses Google Cloud Firestore with custom converters:

- **FirestoreDocument** (Tumult): Base class for all database documents
- **PolymorphicFirestoreDocument** (Tumult): Supports polymorphic serialization via TypeDiscriminator
- **Game** (SpaceWar): Main game state document containing players, board hexes, tech market, event stack
- **GamePlayer** (SpaceWar): Player state (linked subcollection within Game)
- **InteractionData** (Tumult base): Polymorphic interaction data stored separately with GUID references

### Service Architecture

- **GameCache\<TGame, TNonDbState\>** (Tumult): Generic in-memory cache of active games keyed by `DocumentReference` and Discord channel ID. `TNonDbState` must be `IDisposable`; `Clear()` calls `Dispose()` on it automatically.
- **GameSyncManager** (Tumult): Provides per-game locking using `AsyncKeyedLocker<DocumentReference>`. No SpaceWar dependencies.
- **NonDbGameState** (SpaceWar): Transient state not persisted to database (e.g., prod timers for player turn reminders). Implements `IDisposable` so `GameCache` can clean it up on eviction.
- **PerOperationGameState** (SpaceWar): Scoped service tracking state during a single operation (e.g., which interactions to create)

### Code Organization

**Tumult project:**
- **Database/**: `FirestoreDocument`, `PolymorphicFirestoreDocument`, `TypeDiscriminator`, `InteractionData` base, `InteractionsHelper`, `GameCache<TGame, TNonDbState>`
  - **InteractionData/**: Framework-level interaction data base classes
  - **Converters/**: Polymorphic Firestore serialization
- **Discord/**: `InteractionDispatcher`, `IInteractionHandler<T>`, `DiscordMultiMessageBuilder`, `InteractionOutcome`
- **GameLogic/**: `GameEventDispatcher`, `IPlayerChoiceEventHandler`, `IEventResolvedHandler`, `GameSyncManager`, `IGameLoader<TGame>`

**SpaceWarDiscordApp project:**
- **GameLogic/**: Core game logic, board utilities, coordinates, constants
  - **Operations/**: Game operations (movement, production, tech, refresh, game flow)
  - **Techs/**: Individual tech card implementations (each tech is a singleton registered at startup)
  - **MapGeneration/**: Map generators for creating game boards
- **Discord/**: Discord bot integration
  - **Commands/**: Slash command implementations
  - **ArgumentConverters/**: Custom parameter converters for slash commands
  - **ChoiceProvider/**: Autocomplete providers for command parameters
- **Database/**: SpaceWar Firestore models and data access layer
  - **GameEvents/**: SpaceWar event types for the game event stack
  - **InteractionData/**: SpaceWar-specific interaction data models (~98 subclasses)
- **ImageGeneration/**: Generates game board images using ImageSharp
- **AI/**: OpenRouter integration for AI-powered features

## Key Patterns

### Turn Structure

Games progress through phases (Setup → Play → Finished). During Play phase:
- `Game.CurrentTurnPlayerIndex` tracks whose turn it is
- `Game.ActionTakenThisTurn`: Tracks if main action taken
- `Game.AnyActionTakenThisTurn`: Tracks if any action (main or free) taken
- Prod timers (player turn reminder timers) tracked in `NonDbGameState` and updated via `ProdOperations.UpdateProdTimers()`

### Tech System

Techs are singletons registered at startup:
- Each tech class extends `Tech` base class
- Techs can provide `AdditionalHandlers` for complex interactions
- Tech market uses different modes (queue, discounting slots) configured via `GameRules.TechMarketMode`

### Operations Pattern

Operations classes contain static methods that:
1. Acquire game lock via GameSyncManager
2. Load game from cache or database
3. Perform game state mutations
4. Persist changes to Firestore
5. Send Discord messages via GameMessageBuilders

### Secrets Management

Secrets stored in `Secrets.json` (not committed):
- `DiscordToken`: Discord bot token
- `FirestoreProjectId`: Google Cloud project ID
- `OpenRouterApiKey`: API key for AI features
- `IsTestEnvironment`: Flag for test/prod behavior
- `UserToMessageErrorsTo`: Discord user ID for error notifications

Use `Secrets-prod.json` for production configuration.

## Code Style

- Always use braces for `if`, `for`, `foreach`, `while`, and similar blocks — never single-line braceless bodies
- Prefer `using` directives over fully qualified type names; most Tumult namespaces are already available via global usings in `TumultGlobalUsings.cs`

## Important Implementation Notes

- Always use `GameSyncManager.Locker.LockAsync(game.DocumentId)` when modifying game state to prevent race conditions (or let `SpaceWarGameLoader.LockGameAsync()` do it via the Tumult dispatcher)
- Discord custom IDs for buttons/selects must be GUIDs that reference InteractionData documents
- Framework types (`InteractionData`, `GameEvent`, dispatchers, interfaces) live in Tumult — import from `Tumult.*` namespaces, not `SpaceWarDiscordApp.*`
- Image assets (dice, icons, fonts) are copied to output directory and loaded at runtime
- The bot updates emoji cache and server tech listings on startup after guild download completes
- All games in Play phase are loaded into GameCache on startup and prod timers are updated
- Tech instances are created once at startup and shared across all games

## Development Workflow

1. Game logic changes typically require updates to:
   - Event classes in `SpaceWarDiscordApp/Database/GameEvents/`
   - Operation methods in `SpaceWarDiscordApp/GameLogic/Operations/`
   - Command classes in `SpaceWarDiscordApp/Discord/Commands/`
   - Event/interaction handlers implementing `IPlayerChoiceEventHandler` or `IInteractionHandler` (interfaces defined in Tumult)

2. Adding new Discord commands:
   - Create command class implementing slash commands
   - Register in Program.cs via `RegisterEverything()`
   - Add argument converters/choice providers if needed

3. Adding new techs:
   - Create class extending `Tech` in `GameLogic/Techs/`
   - Implement required abstract members
   - Tech automatically registered at startup via reflection
