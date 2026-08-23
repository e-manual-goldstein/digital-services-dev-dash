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
