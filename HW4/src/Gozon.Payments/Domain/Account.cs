namespace Gozon.Payments.Domain;

/// <summary>
/// Счёт пользователя
/// </summary>
public sealed class Account
{
    public long UserId { get; set; }
    public decimal Balance { get; set; }
}