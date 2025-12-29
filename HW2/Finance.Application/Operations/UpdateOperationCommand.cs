using System;
using Finance.Domain;

namespace Finance.Application.Operations;

/// <summary>
/// Команда обновления операции.
/// </summary>
public sealed record UpdateOperationCommand(
    Guid Id,
    Guid AccountId,
    Guid CategoryId,
    decimal Amount,
    Currency Currency,
    OperationType Type,
    DateOnly Date,
    string? Description = null);
