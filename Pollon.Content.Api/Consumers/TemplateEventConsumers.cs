using Pollon.Content.Api.Domain.Interfaces;
using Pollon.Contracts.Events;
using Pollon.Publication.Models;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Pollon.Content.Api.Consumers;

public partial class TemplateEventConsumers
{
    private readonly IContentTemplateRepository _templateRepository;
    private readonly ILogger<TemplateEventConsumers> _logger;

    public TemplateEventConsumers(IContentTemplateRepository templateRepository, ILogger<TemplateEventConsumers> logger)
    {
        _templateRepository = templateRepository;
        _logger = logger;
    }

    public async Task Handle(TemplatePublishedEvent message)
    {
        LogReceivedPublishedEvent(_logger, message.Id, message.FileName);

        var existing = await _templateRepository.GetByIdAsync(message.Id);

        if (existing != null)
        {
            if (message.UpdatedAt < existing.UpdatedAt)
            {
                LogSkippingOutofOrder(_logger, message.Id, message.UpdatedAt, existing.UpdatedAt);
                return;
            }

            LogUpdatingExistingTemplate(_logger, message.Id, message.FileName);

            existing.Name = message.Name;
            existing.DisplayName = message.DisplayName;
            existing.Description = message.Description;
            existing.FileName = message.FileName;
            existing.PreviewImageUrl = message.PreviewImageUrl;
            existing.TemplateContent = message.TemplateContent;
            existing.Variables = message.Variables;
            existing.IsActive = message.IsActive;
            existing.AssociatedContentTypeId = message.AssociatedContentTypeId;
            existing.Tags = message.Tags;
            existing.CreatedAt = message.CreatedAt;
            existing.UpdatedAt = message.UpdatedAt;

            await _templateRepository.UpdateAsync(existing);
        }
        else
        {
            LogCreatingNewTemplate(_logger, message.Id, message.FileName);

            var template = new ContentTemplate
            {
                Id = message.Id,
                Name = message.Name,
                DisplayName = message.DisplayName,
                Description = message.Description,
                FileName = message.FileName,
                PreviewImageUrl = message.PreviewImageUrl,
                TemplateContent = message.TemplateContent,
                Variables = message.Variables,
                IsActive = message.IsActive,
                AssociatedContentTypeId = message.AssociatedContentTypeId,
                Tags = message.Tags,
                CreatedAt = message.CreatedAt,
                UpdatedAt = message.UpdatedAt
            };

            await _templateRepository.AddAsync(template);
        }

        LogSavedSuccess(_logger, message.Id);
    }

    public async Task Handle(TemplateDeletedEvent message)
    {
        LogReceivedDeletedEvent(_logger, message.Id);

        var existing = await _templateRepository.GetByIdAsync(message.Id);

        if (existing != null)
        {
            await _templateRepository.DeleteAsync(existing);
            LogDeletedSuccess(_logger, message.Id);
        }
        else
        {
            LogTemplateNotFoundForDeletion(_logger, message.Id);
        }
    }
}
