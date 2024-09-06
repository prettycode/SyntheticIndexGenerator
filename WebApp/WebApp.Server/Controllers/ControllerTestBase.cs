using Data.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace WebService.Controllers;

public class ControllerTestBase
{
    protected TController GetController<TController>() where TController : ControllerBase
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false)
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddConfiguration(configuration.GetSection("Logging")).AddConsole())
            .AddDataLibraryConfiguration(configuration)
            .AddTransient<TController>()
            .BuildServiceProvider();

        return serviceProvider.GetService<TController>()!;
    }
}
