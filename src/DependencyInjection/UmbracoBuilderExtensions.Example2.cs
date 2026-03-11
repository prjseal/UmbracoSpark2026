using Site.Services;

namespace Site.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder ConfigureExampleTwo(this IUmbracoBuilder builder)
    {
        // add the required services for example two
        builder.Services
            .AddSingleton<IPersonService, PersonService>()
            .AddSingleton<IPersonIndexingService, PersonIndexingService>();

        return builder;
    }
}











