# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SpaceWar is a Discord bot for asynchronous play of a turn-based board game. Players interact with the game through Discord slash commands and button interactions, with game state persisted to Google Cloud Firestore.

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

### Event-Driven Game System

The game uses an event stack model where game actions push events onto `Game.EventStack`. Events are processed sequentially:

- **GameEvent**: Base class for all game events
- **GameEvent_PlayerChoice<TInteractionData>**: Events requiring player input via Discord interactions
- **GameEventDispatcher**: Routes events to registered handlers implementing:
  - `IPlayerChoiceEventHandler<TEvent, TInteractionData>`: Handles player choice events
  - `IEventResolvedHandler<TEvent>`: Handles event resolution after choices are made

### Discord Interaction System

Discord interactions (button clicks, slash commands) are handled through:

- **InteractionDispatcher**: Routes Discord interactions to registered handlers
- **IInteractionHandler<TInteractionData>**: Interface for handling specific interaction types
- **InteractionData**: Database models stored in Firestore with GUIDs used as Discord customId values

All handlers (Commands, Operations, Techs) are registered via `RegisterEverything()` in Program.cs, which registers them with both InteractionDispatcher and GameEventDispatcher.

### Database Architecture

Uses Google Cloud Firestore with custom converters:

- **FirestoreDocument**: Base class for all database documents
- **PolymorphicFirestoreDocument**: Supports polymorphic serialization via TypeDiscriminator
- **Game**: Main game state document containing players, board hexes, tech market, event stack
- **GamePlayer**: Player state (linked subcollection within Game)
- **InteractionData**: Polymorphic interaction data stored separately with GUID references

### Service Architecture

- **GameCache**: In-memory cache of active games to reduce Firestore reads
- **GameSyncManager**: Provides per-game locking using AsyncKeyedLock to prevent race conditions
- **PerOperationGameState**: Scoped service tracking state during a single operation (e.g., which interactions to create)
- **NonDbGameState**: Transient state not persisted to database (e.g., prod timers for player turn reminders)

### Code Organization

- **GameLogic/**: Core game logic, board utilities, coordinates, constants
  - **Operations/**: Game operations (movement, production, tech, refresh, game flow)
  - **Techs/**: Individual tech card implementations (each tech is a singleton registered at startup)
  - **MapGeneration/**: Map generators for creating game boards
- **Discord/**: Discord bot integration
  - **Commands/**: Slash command implementations
  - **ArgumentConverters/**: Custom parameter converters for slash commands
  - **ChoiceProvider/**: Autocomplete providers for command parameters
- **Database/**: Firestore models and data access layer
  - **GameEvents/**: Event types for the game event stack
  - **InteractionData/**: Interaction data models
  - **Converters/**: Firestore custom type converters
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

## Important Implementation Notes

- Always use `GameSyncManager.Locker.LockAsync(game.DocumentId)` when modifying game state to prevent race conditions
- Discord custom IDs for buttons/selects must be GUIDs that reference InteractionData documents
- Image assets (dice, icons, fonts) are copied to output directory and loaded at runtime
- The bot updates emoji cache and server tech listings on startup after guild download completes
- All games in Play phase are loaded into GameCache on startup and prod timers are updated
- Tech instances are created once at startup and shared across all games

## Development Workflow

1. Game logic changes typically require updates to:
   - Event classes in `Database/GameEvents/`
   - Operation methods in `GameLogic/Operations/`
   - Command classes in `Discord/Commands/`
   - Event/interaction handlers implementing IPlayerChoiceEventHandler or IInteractionHandler

2. Adding new Discord commands:
   - Create command class implementing slash commands
   - Register in Program.cs via `RegisterEverything()`
   - Add argument converters/choice providers if needed

3. Adding new techs:
   - Create class extending `Tech` in `GameLogic/Techs/`
   - Implement required abstract members
   - Tech automatically registered at startup via reflection
