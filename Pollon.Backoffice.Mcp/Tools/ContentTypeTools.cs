using System.ComponentModel;
using ModelContextProtocol.Server;
using Pollon.Publication.Models;
using Pollon.Backoffice.Repositories;

namespace Pollon.Backoffice.Mcp.Tools;

[McpServerToolType]
public class ContentTypeTools(IRepository<ContentType> repository)
{
    [McpServerTool, Description("Get all available content types.")]
    public async Task<IEnumerable<ContentType>> GetContentTypes()
        => await repository.GetAllAsync();

    [McpServerTool, Description("Get a single content type by its ID.")]
    public async Task<ContentType?> GetContentTypeById(
        [Description("The unique ID of the content type.")] string id)
        => await repository.GetByIdAsync(id);

    [McpServerTool, Description("Create a new content type.")]
    public async Task CreateContentType(
        [Description("The content type to create.")] ContentType contentType)
        => await repository.CreateAsync(contentType);

    [McpServerTool, Description("Update an existing content type by ID.")]
    public async Task UpdateContentType(
        [Description("The ID of the content type to update.")] string id,
        [Description("The updated content type data.")] ContentType contentType)
        => await repository.UpdateAsync(id, contentType);

    [McpServerTool, Description("Delete a content type by ID.")]
    public async Task DeleteContentType(
        [Description("The ID of the content type to delete.")] string id)
        => await repository.DeleteAsync(id);
}
