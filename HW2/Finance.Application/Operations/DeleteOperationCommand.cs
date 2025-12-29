using System;

namespace Finance.Application.Operations;

/// <summary>
/// Команда удаления операции.
/// </summary>
public sealed record DeleteOperationCommand(Guid Id);
