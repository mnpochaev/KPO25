using Microsoft.Extensions.DependencyInjection;
using ZooErp.Application.Abstractions;
using ZooErp.Application.Services;
using ZooErp.ConsoleApp;
using ZooErp.Domain.Abstractions;
using ZooErp.Infrastructure.Storage;
using ZooErp.Infrastructure.Vet;

var services = new ServiceCollection();

services.AddSingleton<IZooRepository, InMemoryZooStore>();
services.AddSingleton<IVetClinic, SimpleVetClinic>();
services.AddSingleton<IZooService, ZooService>();
services.AddSingleton<ConsoleMenu>();

using var provider = services.BuildServiceProvider();
var menu = provider.GetRequiredService<ConsoleMenu>();

menu.Run();
