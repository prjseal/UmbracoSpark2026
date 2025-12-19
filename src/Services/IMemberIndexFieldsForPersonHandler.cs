using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Site.Services;

public interface IMemberIndexFieldsForPersonHandler
{
    Task<IEnumerable<IndexField>> GetIndexFieldsAsync(IMember member);
}