using Site.Services;

namespace Site.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder ConfigureExampleTwo(this IUmbracoBuilder builder)
    {
        // add the required services for example two
        builder.Services
            .AddSingleton<IPeopleService, PeopleService>()
            .AddSingleton<IPeopleIndexingService, PeopleIndexingService>();

        return builder;
    }
}











