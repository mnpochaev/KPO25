using ZooErp.Application.Abstractions;
using ZooErp.Domain.Zoo;

namespace ZooErp.Infrastructure.Storage;

public class InMemoryZooStore : IZooRepository
{
    private readonly Zoo _zoo = new();

    public Zoo GetZoo() => _zoo;
}
