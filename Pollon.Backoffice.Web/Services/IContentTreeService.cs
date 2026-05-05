using MudBlazor;
using Pollon.Publication.Models;

namespace Pollon.Backoffice.Web.Services
{
    public interface IContentTreeService
    {
        List<TreeItemData<ContentItem>> BuildTree(IEnumerable<ContentItem> items);
        List<TreeItemData<ContentItem>> BuildParentTree(IEnumerable<ContentItem> items, string? excludedId);
        List<TreeItemData<ContentItem>> ApplySearch(IEnumerable<TreeItemData<ContentItem>>? roots, string search);
        void ExpandPathTo(IEnumerable<TreeItemData<ContentItem>> items, string id);

        string GetItemIcon(ContentItem ci);
    }
}
