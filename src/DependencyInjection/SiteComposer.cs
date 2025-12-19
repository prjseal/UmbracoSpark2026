using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

namespace Site.DependencyInjection;

public sealed class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder
            // add core services for search abstractions
            .AddSearchCore()
            // add the Examine search provider
            .AddExamineSearchProvider();

        // force rebuild indexes after startup (awaiting a better solution from Core)
        // builder.RebuildAllIndexesAfterStartup();

        // TODO: split builder extensions into RegisterExampleOne(), RegisterExampleTwo(), ...
        
        builder
            .RegisterServices()
            .RegisterIndexes()
            .ConfigureExamineSearchProvider();
    }
}