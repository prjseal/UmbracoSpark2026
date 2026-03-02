namespace Site.Services;

public interface IPersonIndexingService
{
    Task RebuildIndexAsync();
}