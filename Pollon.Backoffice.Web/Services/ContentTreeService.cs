using MudBlazor;
using Pollon.Publication.Models;
using System.Text.Json;

namespace Pollon.Backoffice.Web.Services
{
    public class ContentTreeService : IContentTreeService
    {
        public List<TreeItemData<ContentItem>> BuildTree(IEnumerable<ContentItem> items)
        {
            var allItems = items.Select(i => new TreeItemData<ContentItem> { Value = i }).ToList();
            var itemLookup = allItems.ToDictionary(i => i.Value!.Id, StringComparer.OrdinalIgnoreCase);
            var roots = new List<TreeItemData<ContentItem>>();

            foreach (var it in allItems)
            {
                var parentId = it.Value!.ParentId;
                if (string.IsNullOrEmpty(parentId) || !itemLookup.TryGetValue(parentId, out var parent))
                {
                    roots.Add(it);
                }
                else
                {
                    if (parent.Children == null) parent.Children = new List<TreeItemData<ContentItem>>();
                    ((List<TreeItemData<ContentItem>>)parent.Children).Add(it);
                }
            }
            return roots;
        }

        public List<TreeItemData<ContentItem>> BuildParentTree(IEnumerable<ContentItem> items, string? excludedId)
        {
            var allItems = items.Select(i => new TreeItemData<ContentItem> { Value = i }).ToList();
            var itemLookup = allItems.ToDictionary(i => i.Value!.Id, StringComparer.OrdinalIgnoreCase);
            
            var excludedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(excludedId))
            {
                excludedIds.Add(excludedId);
                AddDescendants(excludedId, allItems, excludedIds);
            }

            var roots = new List<TreeItemData<ContentItem>>();
            foreach (var it in allItems)
            {
                if (excludedIds.Contains(it.Value!.Id)) continue;

                var parentId = it.Value!.ParentId;
                if (string.IsNullOrEmpty(parentId) || !itemLookup.TryGetValue(parentId, out var parent) || excludedIds.Contains(parentId))
                {
                    roots.Add(it);
                }
                else
                {
                    if (parent.Children == null) parent.Children = new List<TreeItemData<ContentItem>>();
                    ((List<TreeItemData<ContentItem>>)parent.Children).Add(it);
                }
            }
            return roots;
        }

        public List<TreeItemData<ContentItem>> ApplySearch(IEnumerable<TreeItemData<ContentItem>>? roots, string search)
        {
            if (roots == null) return new();
            if (string.IsNullOrWhiteSpace(search)) return roots.ToList();

            var filtered = new List<TreeItemData<ContentItem>>();
            foreach (var root in roots)
            {
                var match = MatchAndClone(root, search);
                if (match != null) filtered.Add(match);
            }
            return filtered;
        }

        public void ExpandPathTo(IEnumerable<TreeItemData<ContentItem>> items, string id)
        {
            foreach (var it in items)
            {
                if (string.Equals(it.Value?.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    it.Expanded = true;
                    return;
                }
                
                if (it.Children != null && it.Children.Any())
                {
                    var found = FindInTree(it.Children, id);
                    if (found != null)
                    {
                        it.Expanded = true;
                        ExpandPathTo(it.Children.Cast<TreeItemData<ContentItem>>(), id);
                        return;
                    }
                }
            }
        }



        public string GetItemIcon(ContentItem ci)
        {
            if (!string.IsNullOrEmpty(ci.Icon)) return ci.Icon;
            return Icons.Material.Filled.Description;
        }

        // --- Private Helpers ---

        private void AddDescendants(string parentId, List<TreeItemData<ContentItem>> allItems, HashSet<string> excludedIds)
        {
            var children = allItems.Where(i => i.Value?.ParentId == parentId).ToList();
            foreach (var child in children)
            {
                excludedIds.Add(child.Value!.Id);
                AddDescendants(child.Value!.Id, allItems, excludedIds);
            }
        }

        private TreeItemData<ContentItem>? MatchAndClone(TreeItemData<ContentItem> node, string search)
        {
            bool nameMatch = node.Value!.GetTitle().Contains(search, StringComparison.OrdinalIgnoreCase);
            
            var filteredChildren = new List<TreeItemData<ContentItem>>();
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var matchChild = MatchAndClone((TreeItemData<ContentItem>)child, search);
                    if (matchChild != null) filteredChildren.Add(matchChild);
                }
            }

            if (nameMatch || filteredChildren.Any())
            {
                return new TreeItemData<ContentItem>
                {
                    Value = node.Value,
                    Expanded = !string.IsNullOrWhiteSpace(search) || node.Expanded,
                    Children = filteredChildren.Any() ? filteredChildren : null
                };
            }
            return null;
        }

        private TreeItemData<ContentItem>? FindInTree(IEnumerable<ITreeItemData<ContentItem>> items, string id)
        {
            foreach (var it in items)
            {
                if (string.Equals(it.Value?.Id, id, StringComparison.OrdinalIgnoreCase)) return (TreeItemData<ContentItem>)it;
                if (it.Children != null)
                {
                    var found = FindInTree(it.Children, id);
                    if (found != null) return found;
                }
            }
            return null;
        }
    }
}
