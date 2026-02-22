using Kjac.SearchProvider.Elasticsearch.DependencyInjection;
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
            // use the Elasticsearch search provider
            .AddElasticsearchSearchProvider()
            // add the Examine search provider
            .AddExamineSearchProvider();
        
        builder
            .RegisterServices()
            .RegisterIndexes()
            .ConfigureExamineSearchProvider();
    }
}