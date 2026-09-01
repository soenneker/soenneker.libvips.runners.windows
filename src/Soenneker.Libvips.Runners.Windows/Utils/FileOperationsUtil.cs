using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Soenneker.GitHub.Repositories.Releases.Abstract;
using Soenneker.Libvips.Runners.Windows.Utils.Abstract;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.File.Abstract;

namespace Soenneker.Libvips.Runners.Windows.Utils;

/// <inheritdoc cref="IFileOperationsUtil"/>
public sealed class FileOperationsUtil : IFileOperationsUtil
{
    private const string Owner = "libvips";
    private const string Repository = "build-win64-mxe";

    private readonly ILogger<FileOperationsUtil> _logger;
    private readonly IDirectoryUtil _directoryUtil;
    private readonly IGitHubRepositoriesReleasesUtil _releasesUtil;
    private readonly IFileUtil _fileUtil;

    public FileOperationsUtil(ILogger<FileOperationsUtil> logger, IDirectoryUtil directoryUtil,
        IGitHubRepositoriesReleasesUtil releasesUtil, IFileUtil fileUtil)
    {
        _logger = logger;
        _directoryUtil = directoryUtil;
        _releasesUtil = releasesUtil;
        _fileUtil = fileUtil;
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

        string distributionDirectory = (await _directoryUtil.GetAllDirectories(extractionDirectory, cancellationToken))
                                       .Single(path => Path.GetFileName(path).StartsWith("vips-dev-", StringComparison.Ordinal));

        string stageDirectory = await _directoryUtil.CreateTempDirectory(cancellationToken);
        await _directoryUtil.CopyDirectory(distributionDirectory, stageDirectory, cancellationToken: cancellationToken);

        string executable = Path.Combine(stageDirectory, "bin", "vips.exe");
        if (!await _fileUtil.Exists(executable, cancellationToken))
            throw new FileNotFoundException("The libvips distribution did not contain bin/vips.exe.", executable);

        _logger.LogInformation("Prepared Windows x64 libvips runtime at {StageDirectory}", stageDirectory);
        return stageDirectory;
    }

}
