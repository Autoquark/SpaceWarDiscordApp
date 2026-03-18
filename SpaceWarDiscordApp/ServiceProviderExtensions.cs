using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database.InteractionData;

namespace SpaceWarDiscordApp;

public static class ServiceProviderExtensions
{
    public static List<InteractionData> GetInteractionsToSetUp(this IServiceProvider serviceProvider)
        => serviceProvider.GetRequiredService<List<InteractionData>>();

    public static string AddInteractionToSetUp(this IServiceProvider serviceProvider, InteractionData interaction)
        => AddInteractionsToSetUp(serviceProvider, interaction).Single();
    
    public static IEnumerable<string> AddInteractionsToSetUp(this IServiceProvider serviceProvider, params IEnumerable<InteractionData> interactions)
    {
        var list = interactions.ToList();
        serviceProvider.GetInteractionsToSetUp().AddRange(list);
        return list.Select(x => x.InteractionId);
    }
}