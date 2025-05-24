# SpaceWar AI Integration

This folder contains the AI integration for the SpaceWar Discord bot, which allows an LLM (GPT-4o mini via OpenRouter) to analyze game states and execute strategic adjustments using fixup commands.

## Setup

### 1. Add OpenRouter API Key to Secrets

Add your OpenRouter API key to the `Secrets.cs` file:

```csharp
internal class Secrets
{
    public string FirestoreProjectId { get; set; } = "";
    public string DiscordToken { get; set; } = "";
    public string OpenRouterApiKey { get; set; } = "";  // Add this line
    public ulong TestGuildId { get; set; } = 0;
}
```

And to your `Secrets.json` file:

```json
{
    "FirestoreProjectId": "your-firestore-project",
    "DiscordToken": "your-discord-token",
    "OpenRouterApiKey": "your-openrouter-api-key",
    "TestGuildId": 123456789
}
```

### 2. Get an OpenRouter API Key

1. Sign up at [OpenRouter.ai](https://openrouter.ai/)
2. Create an API key in your account settings
3. Add the key to your secrets configuration

## Usage

### Basic Usage

```csharp
// Create the AI service
var aiService = AIServiceFactory.CreateAIService("your-openrouter-api-key");

// Analyze a game and let AI make adjustments
var result = await aiService.AnalyzeGameStateAndExecuteActionsAsync(
    game, 
    contextPlayer, 
    "Make the game more competitive"
);

// Display results
Console.WriteLine(result.GetSummary());
```

### In a Discord Command

```csharp
[Command("analyze")]
public static async Task AnalyzeGameCommand(CommandContext context, string? prompt = null)
{
    var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
    var contextPlayer = game.GetGamePlayerByDiscordId(context.User.Id);
    
    var aiService = AIServiceFactory.CreateAIService(secrets.OpenRouterApiKey);
    var result = await aiService.AnalyzeGameStateAndExecuteActionsAsync(game, contextPlayer, prompt);
    
    await context.RespondAsync(result.GetSummary());
}
```

## Architecture

### Components

1. **ToolDefinitions.json** - JSON function definitions for OpenAI function calling
2. **Models/** - Data models for API communication and tool parameters
3. **Services/**
   - `OpenRouterService` - Handles API communication with OpenRouter
   - `FixupCommandExecutor` - Executes fixup commands without Discord context
   - `SpaceWarAIService` - Main orchestration service

### Available Tools

The AI can use the following tools to modify game state:

- `setForces` - Modify force counts and ownership on planets
- `grantTech` - Give technologies to players
- `removeTech` - Remove technologies from players
- `setTechExhausted` - Change tech exhaustion state
- `setPlanetExhausted` - Change planet exhaustion state
- `setPlayerTurn` - Change whose turn it is
- `setPlayerScience` - Modify player science totals
- `setPlayerVictoryPoints` - Modify player victory points

### AI Behavior

The AI is designed to:
- Analyze current game balance and player positions
- Consider strategic implications of technologies and resources
- Suggest modifications to improve game balance or test scenarios
- Explain its reasoning for any changes made

## Example Prompts

- "Analyze the current game state and suggest strategic adjustments"
- "Make the game more competitive by balancing player resources"
- "Give player 2 some advantages to catch up to the leader"
- "Test what would happen if all planets were refreshed"
- "Simulate the effects of giving the current player a military tech"

## Error Handling

The system includes comprehensive error handling:
- Invalid tool parameters are caught and reported
- API failures are gracefully handled
- Game state validation prevents invalid modifications
- All operations are logged for debugging

## Security Considerations

- Tool execution requires a valid game context
- Player validation ensures only valid players can be targeted
- Coordinate validation prevents invalid planet modifications
- All changes are persisted to Firestore with transaction safety 