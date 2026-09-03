using System.Diagnostics;
using System.Reflection;
using DigitalDevServices.Model.Applications;

namespace DigitalDevServices.Services.Applications;

public sealed class DeployedPackageService : IDeployedPackageService
{
    private readonly IApplicationInstanceService _applicationInstanceService;

    public DeployedPackageService(IApplicationInstanceService applicationInstanceService)
    {
        _applicationInstanceService = applicationInstanceService;
    }

    public async Task<DeployedPackageScanResult> ScanAsync(
        Guid applicationInstanceId,
        CancellationToken cancellationToken = default)
    {
        var instance = await _applicationInstanceService
            .GetByIdAsync(applicationInstanceId, cancellationToken)
            .ConfigureAwait(false);

        if (instance is null)
        {
            return new DeployedPackageScanResult
            {
                ErrorMessage = "Application instance was not found."
            };
        }

        var physicalPath = instance.PhysicalPath?.Trim();
        if (string.IsNullOrWhiteSpace(physicalPath))
        {
            return new DeployedPackageScanResult
            {
                ErrorMessage = "No physical path is configured for this deployment."
            };
        }

        if (!Directory.Exists(physicalPath))
        {
            return new DeployedPackageScanResult
            {
                ErrorMessage = $"Deploy folder does not exist: {physicalPath}"
            };
        }

        var manifestPath = Path.Combine(physicalPath, DeploymentManifestParser.ManifestFileName);
        if (File.Exists(manifestPath))
        {
            var manifestResult = DeploymentManifestParser.ParseFile(manifestPath);
            if (manifestResult.CouldReadFile && manifestResult.Packages.Count > 0)
            {
                return new DeployedPackageScanResult
                {
                    Source = DeployedPackageSource.Manifest,
                    ManifestFileName = DeploymentManifestParser.ManifestFileName,
                    Packages = manifestResult.Packages,
                    Warnings = manifestResult.Warnings
                };
            }

            var filesystemResult = ScanFilesystem(physicalPath, cancellationToken);
            var warnings = manifestResult.Warnings.ToList();

            if (!manifestResult.CouldReadFile)
            {
                warnings.Add($"Fell back to filesystem scan because {DeploymentManifestParser.ManifestFileName} could not be read.");
            }
            else if (manifestResult.Packages.Count == 0)
            {
                warnings.Add($"Fell back to filesystem scan because {DeploymentManifestParser.ManifestFileName} contained no package entries.");
            }

            return new DeployedPackageScanResult
            {
                Source = filesystemResult.Source,
                Packages = filesystemResult.Packages,
                Warnings = warnings,
                ErrorMessage = filesystemResult.ErrorMessage
            };
        }

        return ScanFilesystem(physicalPath, cancellationToken);
    }

    public async Task<DeployedPackageComparisonResult> CompareInstancesAsync(
        Guid leftInstanceId,
        Guid rightInstanceId,
        CancellationToken cancellationToken = default)
    {
        if (leftInstanceId == rightInstanceId)
        {
            return new DeployedPackageComparisonResult
            {
                LeftInstanceId = leftInstanceId,
                RightInstanceId = rightInstanceId,
                ErrorMessage = "Choose two different application instances to compare."
            };
        }

        var leftInstance = await _applicationInstanceService
            .GetByIdAsync(leftInstanceId, cancellationToken)
            .ConfigureAwait(false);
        var rightInstance = await _applicationInstanceService
            .GetByIdAsync(rightInstanceId, cancellationToken)
            .ConfigureAwait(false);

        if (leftInstance is null || rightInstance is null)
        {
            return new DeployedPackageComparisonResult
            {
                LeftInstanceId = leftInstanceId,
                RightInstanceId = rightInstanceId,
                ErrorMessage = "One or both application instances were not found."
            };
        }

        if (leftInstance.DeployableApplicationId != rightInstance.DeployableApplicationId)
        {
            return new DeployedPackageComparisonResult
            {
                LeftInstanceId = leftInstanceId,
                RightInstanceId = rightInstanceId,
                ErrorMessage = "Both instances must belong to the same deployable application."
            };
        }

        var leftScan = await ScanAsync(leftInstanceId, cancellationToken).ConfigureAwait(false);
        var rightScan = await ScanAsync(rightInstanceId, cancellationToken).ConfigureAwait(false);

        if (!leftScan.IsSuccess)
        {
            return new DeployedPackageComparisonResult
            {
                LeftInstanceId = leftInstanceId,
                RightInstanceId = rightInstanceId,
                LeftScan = leftScan,
                RightScan = rightScan,
                ErrorMessage = $"Instance A: {leftScan.ErrorMessage}"
            };
        }

        if (!rightScan.IsSuccess)
        {
            return new DeployedPackageComparisonResult
            {
                LeftInstanceId = leftInstanceId,
                RightInstanceId = rightInstanceId,
                LeftScan = leftScan,
                RightScan = rightScan,
                ErrorMessage = $"Instance B: {rightScan.ErrorMessage}"
            };
        }

        return new DeployedPackageComparisonResult
        {
            LeftInstanceId = leftInstanceId,
            RightInstanceId = rightInstanceId,
            LeftScan = leftScan,
            RightScan = rightScan,
            Rows = DeployedPackageComparer.Compare(leftScan, rightScan)
        };
    }

    private static DeployedPackageScanResult ScanFilesystem(
        string physicalPath,
        CancellationToken cancellationToken)
    {
        var packages = new List<DeployedPackageInfo>();

        try
        {
            foreach (var dllPath in Directory.EnumerateFiles(physicalPath, "*.dll", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                packages.Add(ReadPackageInfo(dllPath));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new DeployedPackageScanResult
            {
                ErrorMessage = $"Could not read packages from '{physicalPath}': {ex.Message}"
            };
        }

        return new DeployedPackageScanResult
        {
            Source = DeployedPackageSource.FilesystemScan,
            Packages = packages
                .OrderBy(package => package.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static DeployedPackageInfo ReadPackageInfo(string dllPath)
    {
        var fileName = Path.GetFileName(dllPath);
        string? fileVersion = null;
        string? assemblyVersion = null;

        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(dllPath);
            if (!string.IsNullOrWhiteSpace(versionInfo.FileVersion))
            {
                fileVersion = versionInfo.FileVersion;
            }
        }
        catch (Exception)
        {
            // Leave file version empty when metadata cannot be read.
        }

        try
        {
            assemblyVersion = AssemblyName.GetAssemblyName(dllPath).Version?.ToString();
        }
        catch (Exception)
        {
            // Leave assembly version empty when the DLL cannot be inspected.
        }

        return new DeployedPackageInfo
        {
            FileName = fileName,
            FileVersion = fileVersion,
            AssemblyVersion = assemblyVersion
        };
    }
}
