namespace Finance.Application.Import;

/// <summary>
/// Фабрика выбора стратегии импорта.
/// </summary>
public interface IImportStrategyFactory
{
    IImportStrategy Resolve(DataFormat format);
}
