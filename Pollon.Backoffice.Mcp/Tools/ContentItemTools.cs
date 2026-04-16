using System.ComponentModel;
using ModelContextProtocol.Server;
using Pollon.Publication.Models;
using Pollon.Backoffice.Services;

namespace Pollon.Backoffice.Mcp.Tools;

[McpServerToolType]
public class ContentItemTools(IContentItemService service)
{
    [McpServerTool, Description("Get all content items. Optionally filter by status (Draft, Published, Archived).")]
    public async Task<IEnumerable<ContentItem>> GetContentItems(
        [Description("Filter by status (Draft, Published, Archived).")] string? status = null,
        [Description("Field to sort by.")] string? sortBy = null)
        => await service.GetAllAsync(status, sortBy);

    [McpServerTool, Description("Get a single content item by its ID.")]
    public async Task<ContentItem?> GetContentItemById(
        [Description("The unique ID of the content item.")] string id)
        => await service.GetByIdAsync(id);

    [McpServerTool, Description("Create a new content item.")]
    public async Task CreateContentItem(
        [Description("The content item to create.")] ContentItem item)
        => await service.CreateAndPublishAsync(item);

    [McpServerTool, Description("Update an existing content item by ID.")]
    public async Task UpdateContentItem(
        [Description("The ID of the content type to update.")] string id,
        [Description("The updated content item data.")] ContentItem item)
        => await service.UpdateAndPublishAsync(id, item);

    [McpServerTool, Description("Delete a content item by ID.")]
    public async Task DeleteContentItem(
        [Description("The ID of the content item to delete.")] string id)
        => await service.DeleteAndPublishAsync(id);
}
