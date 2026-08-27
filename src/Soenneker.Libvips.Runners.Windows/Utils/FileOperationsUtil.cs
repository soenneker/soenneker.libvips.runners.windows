using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Soenneker.GitHub.Repositories.Releases.Abstract;
using Soenneker.Libvips.Runners.Windows.Utils.Abstract;
using Soenneker.Utils.Directory.Abstract;

namespace Soenneker.Libvips.Runners.Windows.Utils;

/// <inheritdoc cref="IFileOperationsUtil"/>
public sealed class FileOperationsUtil : IFileOperationsUtil
{
    private const string Owner = "libvips";
    private const string Repository = "build-win64-mxe";

    private readonly ILogger<FileOperationsUtil> _logger;
    private readonly IDirectoryUtil _directoryUtil;
    private readonly IGitHubRepositoriesReleasesUtil _releasesUtil;

    public FileOperationsUtil(ILogger<FileOperationsUtil> logger, IDirectoryUtil directoryUtil,
        IGitHubRepositoriesReleasesUtil releasesUtil)
    {
        _logger = logger;
        _directoryUtil = directoryUtil;
        _releasesUtil = releasesUtil;
    }

    public async ValueTask<string> Process(CancellationToken cancellationToken = default)
    {
        string downloadDirectory = await _directoryUtil.CreateTempDirectory(cancellationToken);
        string? asset = await _releasesUtil.DownloadReleaseAssetByNamePattern(Owner, Repository, downloadDirectory,
            ["vips-dev-x64-web-", ".zip"], cancellationToken);

        if (asset is null)
            throw new FileNotFoundException($"Could not find a stable x64 web distribution in the latest {Repository} release.");

        string extractionDirectory = await _directoryUtil.CreateTempDirectory(cancellationToken);
        ZipFile.ExtractToDirectory(asset, extractionDirectory);

        string distributionDirectory = Directory.EnumerateDirectories(extractionDirectory, "vips-dev-*", SearchOption.TopDirectoryOnly)
                                                .Single();

        string stageDirectory = await _directoryUtil.CreateTempDirectory(cancellationToken);
        CopyDirectory(distributionDirectory, stageDirectory);

        string executable = Path.Combine(stageDirectory, "bin", "vips.exe");
        if (!File.Exists(executable))
            throw new FileNotFoundException("The libvips distribution did not contain bin/vips.exe.", executable);

        _logger.LogInformation("Prepared Windows x64 libvips runtime at {StageDirectory}", stageDirectory);
        return stageDirectory;
    }

    private static void CopyDirectory(string sourceRoot, string destinationRoot)
    {
        foreach (string directory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, directory)));

        foreach (string file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            string destination = Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, true);
        }
    }
}
