using ZooErp.Application.Models;

namespace ZooErp.Application.Abstractions;

public interface IZooService
{
    bool AcceptAnimal(AddAnimalRequest request);

    void RegisterThing(AddThingRequest request);

    ZooReportDto BuildReports();
}
