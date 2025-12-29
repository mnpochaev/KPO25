using ZooErp.Domain.Abstractions;
using ZooErp.Domain.Animals;

namespace ZooErp.Infrastructure.Vet;

public class SimpleVetClinic : IVetClinic
{
    private readonly Random _random;

    public SimpleVetClinic(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public bool IsHealthy(Animal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);
        return _random.NextDouble() >= 0.2;
    }
}
