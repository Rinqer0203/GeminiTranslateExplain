using GeminiTranslateExplain.Models;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace GeminiTranslateExplain.Services
{
    public sealed class AppUpdateService
    {
        private const string DefaultUpdateRepositoryUrl = "https://github.com/Rinqer0203/GeminiTranslateExplain";

        public static AppUpdateService Instance { get; } = new();

        private readonly SemaphoreSlim _updateLock = new(1, 1);

        private AppUpdateService()
        {
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync(Action<int>? progress = null)
        {
            await _updateLock.WaitAsync();
            try
            {
                var manager = CreateUpdateManager();
                if (!manager.IsInstalled)
                    return UpdateCheckResult.NotManagedByVelopack;

                var updateInfo = await manager.CheckForUpdatesAsync();
                if (updateInfo == null)
                    return UpdateCheckResult.UpToDate;

                await manager.DownloadUpdatesAsync(updateInfo, progress);
                return UpdateCheckResult.UpdateReady(updateInfo.TargetFullRelease.Version.ToString());
            }
            finally
            {
                _updateLock.Release();
            }
        }

        public async Task ApplyPendingUpdateAndRestartAsync()
        {
            var manager = CreateUpdateManager();
            var pending = manager.UpdatePendingRestart;
            if (pending == null)
                return;

            AppConfig.Instance.SaveConfigJson();
            await manager.WaitExitThenApplyUpdatesAsync(pending, silent: false, restart: true);
            System.Windows.Application.Current.Shutdown();
        }

        private static UpdateManager CreateUpdateManager()
        {
            return new UpdateManager(new GithubSource(DefaultUpdateRepositoryUrl, accessToken: null, prerelease: false));
        }
    }

    public sealed record UpdateCheckResult(UpdateCheckStatus Status, string? Version = null)
    {
        public static UpdateCheckResult NotManagedByVelopack { get; } = new(UpdateCheckStatus.NotManagedByVelopack);
        public static UpdateCheckResult UpToDate { get; } = new(UpdateCheckStatus.UpToDate);

        public static UpdateCheckResult UpdateReady(string version) => new(UpdateCheckStatus.UpdateReady, version);
    }

    public enum UpdateCheckStatus
    {
        NotManagedByVelopack,
        UpToDate,
        UpdateReady
    }
}
