using System;

namespace Pollon.Contracts.Events;

public record ContentSubmittedEvent(
    string SubmissionId,
    string ContentItemId,
    string SystemName,
    string? UserId,
    string? UserName,
    string JsonData
);
