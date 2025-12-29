using ZooErp.Domain.Animals;

namespace ZooErp.Domain.Abstractions;

public interface IVetClinic
{
    bool IsHealthy(Animal animal);
}
