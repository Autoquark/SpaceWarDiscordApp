using Microsoft.Extensions.DependencyInjection;
using Tumult.Database.Interactions;

namespace Tumult;

public static class ServiceProviderExtensions
{
    public static List<InteractionData> GetInteractionsToSetUp(this IServiceProvider serviceProvider)
        => serviceProvider.GetRequiredService<List<InteractionData>>();

    public static string AddInteractionToSetUp(this IServiceProvider serviceProvider, InteractionData interaction)
        =>
            serviceProvider.AddInteractionsToSetUp(interaction).Single();

    public static IEnumerable<string> AddInteractionsToSetUp(this IServiceProvider serviceProvider, params IEnumerable<InteractionData> interactions)
    {
        var list = interactions.ToList();
        serviceProvider.GetInteractionsToSetUp().AddRange(list);
        return list.Select(x => x.InteractionId);
    }
}
