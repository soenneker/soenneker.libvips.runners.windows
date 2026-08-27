using Microsoft.Extensions.DependencyInjection;
using Soenneker.GitHub.Repositories.Releases.Registrars;
using Soenneker.Libvips.Runners.Windows.Utils;
using Soenneker.Libvips.Runners.Windows.Utils.Abstract;
using Soenneker.Managers.Runners.Registrars;
using Soenneker.Utils.Directory.Registrars;

namespace Soenneker.Libvips.Runners.Windows;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddHostedService<ConsoleHostedService>()
            .AddSingleton<IFileOperationsUtil, FileOperationsUtil>()
            .AddDirectoryUtilAsSingleton()
            .AddGitHubRepositoriesReleasesUtilAsSingleton()
            .AddRunnersManagerAsSingleton();
    }
}
