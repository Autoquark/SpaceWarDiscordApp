using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.AI.Services;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord.ContextChecks;

namespace SpaceWarDiscordApp.AI;

/// <summary>
/// Example Discord commands for AI integration. 
/// To use these commands, you need to:
/// 1. Add OpenRouterApiKey to your Secrets.cs and Secrets.json
/// 2. Register these commands in Program.cs by adding: extension.AddCommands(typeof(AICommands));
/// </summary>
[Command("ai")]
[RequireGameChannel]
public class AICommands
{
    /// <summary>
    /// Have the AI analyze the current game state and suggest/make strategic adjustments
    /// </summary>
    [Command("analyze")]
    [Description("Have AI analyze the game state and suggest strategic adjustments")]
    public static async Task AnalyzeGameCommand(
        CommandContext context,
        [Description("Custom prompt for the AI analysis")] string? prompt = null)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var contextPlayer = game.GetGamePlayerByDiscordId(context.User.Id);
        
        try
        {
            // Note: This assumes you have added OpenRouterApiKey to Secrets.cs
            // You would need to modify Program.cs to make the secrets available here
            // For now, this shows the pattern of how to use the AI service
            
            // Example of how you would create the service if you had access to secrets:
            // var secrets = context.ServiceProvider.GetRequiredService<Secrets>();
            // var aiService = AIServiceFactory.CreateAIService(secrets.OpenRouterApiKey);
            
            // Placeholder implementation - you would replace this with actual service creation
            await context.RespondAsync("⚠️ AI Analysis is not yet configured. Please add OpenRouterApiKey to your Secrets configuration and update Program.cs to inject the AI service.");
            
            /*
            // This is what the working implementation would look like:
            var aiService = AIServiceFactory.CreateAIService(secrets.OpenRouterApiKey);
            var result = await aiService.AnalyzeGameStateAndExecuteActionsAsync(game, contextPlayer, prompt);
            
            if (result.Success)
            {
                await context.RespondAsync($"🤖 **AI Analysis Complete**\n\n{result.GetSummary()}");
            }
            else
            {
                await context.RespondAsync($"❌ **AI Analysis Failed**\n{result.Error}");
            }
            */
        }
        catch (Exception ex)
        {
            await context.RespondAsync($"❌ Error during AI analysis: {ex.Message}");
        }
    }

    /// <summary>
    /// Have the AI analyze the game but only provide suggestions without making changes
    /// </summary>
    [Command("suggest")]
    [Description("Have AI analyze the game and provide suggestions without making changes")]
    public static async Task SuggestCommand(
        CommandContext context,
        [Description("Custom prompt for the AI suggestions")] string? prompt = null)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        
        try
        {
            // This would be the pattern for a suggestions-only command
            // You would modify the AI service to have a "suggest only" mode
            
            await context.RespondAsync("⚠️ AI Suggestions are not yet configured. This command would provide analysis without making changes to the game state.");
            
            /*
            // Working implementation would look like:
            var aiService = AIServiceFactory.CreateAIService(secrets.OpenRouterApiKey);
            
            // Create a modified request that doesn't allow tool calls
            var result = await aiService.AnalyzeGameStateAsync(game, prompt); // Hypothetical method without tool execution
            
            await context.RespondAsync($"🤖 **AI Suggestions**\n\n{result.AIResponse}");
            */
        }
        catch (Exception ex)
        {
            await context.RespondAsync($"❌ Error during AI suggestion: {ex.Message}");
        }
    }
}

/// <summary>
/// Helper class showing how to integrate AI services with dependency injection
/// This demonstrates the pattern you would use in Program.cs
/// </summary>
public static class AIServiceRegistration
{
    /// <summary>
    /// Example of how to register AI services in dependency injection container
    /// Add this to Program.cs in the ConfigureServices section
    /// </summary>
    public static void RegisterAIServices(IServiceCollection services, string openRouterApiKey)
    {
        services.AddSingleton<HttpClient>();
        services.AddSingleton(provider => 
        {
            var httpClient = provider.GetRequiredService<HttpClient>();
            return new OpenRouterService(httpClient, openRouterApiKey);
        });
        services.AddSingleton<FixupCommandExecutor>();
        services.AddSingleton<SpaceWarAIService>();
    }
} 