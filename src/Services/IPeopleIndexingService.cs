namespace Site.Services;

public interface IPeopleIndexingService
{
    Task RebuildIndexAsync();
}