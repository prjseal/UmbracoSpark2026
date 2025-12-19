using System.Text.Json.Serialization.Metadata;
using Site.ContentIndexing;
using Site.NotificationHandlers;
using Site.Services;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;

namespace Site.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder ConfigureExamineSearchProvider(this IUmbracoBuilder builder)
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
                    PropertyName = SiteConstants.FieldNames.Zodiac,
                    FieldValues = FieldValues.Keywords,
                    Facetable = true
                },
                new ()
                {
                    PropertyName = SiteConstants.FieldNames.Genre,
                    FieldValues = FieldValues.Keywords,
                    Facetable = true
                },
                new ()
                {
                    PropertyName = SiteConstants.FieldNames.Personality,
                    FieldValues = FieldValues.Keywords,
                    Facetable = true
                },
                new ()
                {
                    PropertyName = SiteConstants.FieldNames.Birthdate,
                    FieldValues = FieldValues.DateTimeOffsets,
                    Facetable = true,
                    Sortable = true
                },
                new ()
                {
                    PropertyName = SiteConstants.FieldNames.Name,
                    FieldValues = FieldValues.Texts,
                    Sortable = true
                },
            ]
        );

        return builder;
    }

    public static IUmbracoBuilder RegisterServices(this IUmbracoBuilder builder)
    {
        builder.Services
            .AddTransient<IContentIndexer, MemberAsPersonContentIndexer>()
            .AddTransient<IMemberContentChangeStrategy, MemberContentChangeStrategy>()
            .AddSingleton<IPeopleService, PeopleService>()
            .AddSingleton<IMemberToPersonService, MemberToPersonService>()
            .AddSingleton<IMemberIndexFieldsForPersonHandler, MemberIndexFieldsForPersonHandler>();

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, UmbracoApplicationStartedNotificationHandler>();

        return builder;
    }
}