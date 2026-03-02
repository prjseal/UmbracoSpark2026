using Umbraco.Cms.Core.Composing;

namespace Site.DependencyInjection;

public sealed class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder
            .ConfigureExampleOne()
            .ConfigureExampleTwo()
            .ConfigureExampleThree();
}