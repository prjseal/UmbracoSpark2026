using Site.Services;

namespace Site.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder ConfigureExampleThree(this IUmbracoBuilder builder)
    {
        // add the required services for example three
        builder.Services
            .AddSingleton<IPersonIndexingService, PersonIndexingService>();

        return builder;
    }
}
