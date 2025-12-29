using System;
using Finance.Domain;

namespace Finance.Application.Operations;

/// <summary>
/// Команда добавления операции.
/// </summary>
public sealed record AddOperationCommand(
    Guid AccountId,
    Guid CategoryId,
    decimal Amount,
    Currency Currency,
    OperationType Type,
    DateOnly? Date = null,
    string? Description = null);
