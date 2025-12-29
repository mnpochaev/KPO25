using Finance.Domain;

namespace Finance.Tests;

public class ValueObjectTests
{
    [Fact]
    public void Money_Не_позволяет_отрицательную_сумму()
    {
        Assert.Throws<ArgumentException>(() => new Money(-1));
    }

    [Fact]
    public void Category_Создается_с_Id()
    {
        var category = new Category("Тест", OperationType.Income);
        Assert.NotEqual(Guid.Empty, category.Id);
    }
}
