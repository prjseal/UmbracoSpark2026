using Kjac.SearchProvider.Elasticsearch.DependencyInjection;
using Kjac.SearchProvider.Elasticsearch.Extensions;
using Site.ContentIndexing;
using Site.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

namespace Site.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder ConfigureExampleOne(this IUmbracoBuilder builder)
    {
        builder
            // add the Umbraco Search core services
            .AddSearchCore()
            // use the Elasticsearch search provider
            .AddElasticsearchSearchProvider()
            // add the Examine search provider
            .AddExamineSearchProvider();

        // configure the Examine search options
        builder.ConfigureExamineSearchProvider();

        // register a custom published content index with the Elasticsearch provider
        builder.Services.Configure<IndexOptions>(options =>
            options.RegisterElasticsearchContentIndex<IPublishedContentChangeStrategy>
            (
                SiteConstants.IndexAliases.CustomIndexElasticsearch,
                UmbracoObjectTypes.Document
            )
        );

        // add the required services for example one
        builder.Services
            .AddSingleton<IRecipeRatingService, RecipeRatingService>()
            .AddTransient<IContentIndexer, RecipeRatingContentIndexer>();

        return builder;
    }

    private static void ConfigureExamineSearchProvider(this IUmbracoBuilder builder)
    {
        // by default, Examine (Lucene) filters out facet values that are not active (picked) within a facet group,
        // if any facet value is active within that facet group.
        // expanding facets changes that behavior to include non-active (valid) facet values in the result.
        // NOTE: this incurs a performance penalty when querying. 
        builder.Services.Configure<SearcherOptions>(options => options.ExpandFacetValues = true);

        // the Examine search provider requires explicit definitions of the fields used for faceting and/or sorting. 
        builder.Services.Configure<FieldOptions>(options => options.Fields =
            [
                new ()
                {
                    PropertyName = "cuisine",
                    FieldValues = FieldValues.Keywords,
                    Facetable = true,
                    Sortable = true
                },
                new ()
                {
                    PropertyName = "mealType",
                    FieldValues = FieldValues.Keywords,
                    Facetable = true
                },
                new ()
                {
                    PropertyName = "preparationTime",
                    FieldValues = FieldValues.Integers,
                    Facetable = true,
                    Sortable = true
                },
                new ()
                {
                    PropertyName = "rating",
                    FieldValues = FieldValues.Decimals,
                    Facetable = false,
                    Sortable = true
                },
            ]
        );
    }
}











