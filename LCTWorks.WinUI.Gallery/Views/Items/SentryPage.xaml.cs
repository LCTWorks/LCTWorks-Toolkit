using LCTWorks.Telemetry;
using LCTWorks.WinUI.Controls;
using LCTWorks.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace LCTWorks.Workshop.Views.Items;

/// <summary>
/// A manual test space for <c>SentryTelemetryServiceInternal</c> (via <see cref="ITelemetryService"/>).
/// Initialize the SDK with a DSN, then trigger logs, breadcrumbs, handled/unhandled errors,
/// traces and flushing. Each action is echoed to an on-page activity log so behavior is visible
/// without leaving the app; the actual events are delivered to your Sentry project.
/// </summary>
public sealed partial class SentryPage : ObservablePage
{
    private const string DsnSettingKey = "SentryTestPage.Dsn";
    private const string EnvironmentSettingKey = "SentryTestPage.Environment";

    private readonly ITelemetryService? _telemetry;

    private string? _childId;
    private bool _isInitialized;
    private string? _transactionId;

    public SentryPage()
    {
        InitializeComponent();
        _telemetry = App.GetService<ITelemetryService>();

        // Restore the DSN/environment entered in a previous session (MSIX builds only).
        var cachedDsn = LocalSettingsHelper.ReadSetting<string>(DsnSettingKey);
        Dsn = string.IsNullOrWhiteSpace(cachedDsn) ? string.Empty : cachedDsn;

        var cachedEnvironment = LocalSettingsHelper.ReadSetting<string>(EnvironmentSettingKey);
        Environment = string.IsNullOrWhiteSpace(cachedEnvironment) ? "development" : cachedEnvironment;

        StatusMessage = _telemetry == null
            ? "ITelemetryService is not registered."
            : string.IsNullOrWhiteSpace(Dsn)
                ? "Not initialized. Enter a DSN and press Initialize."
                : "Not initialized. A saved DSN was restored — press Initialize.";
        StatusSeverity = _telemetry == null ? InfoBarSeverity.Error : InfoBarSeverity.Informational;
    }

    public ObservableCollection<string> Activity { get; } = [];

    public string Dsn
    {
        get => field;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string Environment
    {
        get => field;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string StatusMessage
    {
        get => field;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public InfoBarSeverity StatusSeverity
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    private static Exception CreateSampleException(string message)
    {
        // Throw and catch so the exception carries a real stack trace.
        try
        {
            throw new InvalidOperationException(message,
                new ArgumentException("Sample inner exception."));
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static string ResolveCachePath()
    {
        try
        {
            return Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
        }
        catch
        {
            var fallback = Path.Combine(Path.GetTempPath(), "LCTWorks.Workshop", "Sentry");
            Directory.CreateDirectory(fallback);
            return fallback;
        }
    }

    private void AppendActivity(string line)
                => Activity.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  {line}");

    private void AppendToTraceTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        if (string.IsNullOrEmpty(_transactionId))
        {
            AppendActivity("[skipped] No active transaction to append data to.");
            return;
        }
        //_telemetry!.AppendToTrace(_transactionId, [("checkpoint", DateTime.Now.ToString("HH:mm:ss"))]);
        AppendActivity("Appended data to the active transaction.");
    }

    private void ClearLogTapped(object sender, TappedRoutedEventArgs e)
        => Activity.Clear();

    private void ConfigureScopeTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        _telemetry!.ConfigureScope([("test-run", Guid.NewGuid().ToString("N")[..8]), ("area", "sentry-test-page")]);
        AppendActivity("Configured scope tags (test-run, area).");
    }

    private void DisposableTraceOkTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        var id = Guid.NewGuid().ToString();
        using (_telemetry!.StartDisposableTrace(id, "Disposable success", "workshop.disposable"))
        {
            // Simulated successful unit of work; disposal finishes with Ok.
        }
        AppendActivity("Ran disposable trace, disposed cleanly (Ok).");
    }

    private void DisposableTraceThrowsTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        var id = Guid.NewGuid().ToString();
        var trace = _telemetry!.StartDisposableTrace(id, "Disposable failure", "workshop.disposable");
        try
        {
            throw new InvalidOperationException("Simulated failure inside a disposable trace.");
        }
        catch (Exception ex)
        {
            trace.Finish(ex);
            AppendActivity("Ran disposable trace that failed; finished with InternalError.");
        }
    }

    private bool EnsureReady()
    {
        if (_telemetry == null)
        {
            AppendActivity("[skipped] ITelemetryService is not available.");
            return false;
        }
        if (!_isInitialized)
        {
            AppendActivity("[skipped] Service not initialized. Press Initialize first.");
            return false;
        }
        return true;
    }

    private void FinishTraceErrorTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        if (string.IsNullOrEmpty(_transactionId))
        {
            AppendActivity("[skipped] No active transaction to finish.");
            return;
        }
        var ex = CreateSampleException("Transaction failed in the test page.");
        //_telemetry!.FinishTrace(_transactionId, TelemetryTraceStatus.InternalError, ex);
        AppendActivity("Finished transaction with status InternalError + exception.");
        _transactionId = null;
        _childId = null;
    }

    private void FinishTraceOkTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        if (string.IsNullOrEmpty(_transactionId))
        {
            AppendActivity("[skipped] No active transaction to finish.");
            return;
        }
        _telemetry!.FinishTrace(_transactionId, TelemetryTraceStatus.Ok);
        AppendActivity("Finished transaction with status Ok (child spans auto-finished).");
        _transactionId = null;
        _childId = null;
    }

    private void FlushTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        _telemetry!.Flush();
        AppendActivity("Flushed queued events (and swept stale spans).");
    }

    private void InitializeTapped(object sender, TappedRoutedEventArgs e)
    {
        if (_telemetry == null)
        {
            AppendActivity("[error] ITelemetryService is not registered in DI.");
            return;
        }
        if (string.IsNullOrWhiteSpace(Dsn))
        {
            StatusMessage = "Enter a Sentry DSN before initializing.";
            StatusSeverity = InfoBarSeverity.Warning;
            AppendActivity("[skipped] Initialize called without a DSN.");
            return;
        }

        try
        {
            var contextData = new TelemetryEnvironmentContextData(
                AppDisplayName: "Workshop",
                AppLocalCachePath: ResolveCachePath(),
                AppVersion: "1.0.0",
                Culture: CultureInfo.CurrentCulture,
                DeviceFamily: "Desktop",
                OsArchitecture: RuntimeInformation.OSArchitecture.ToString(),
                OsName: "Windows",
                OsVersion: System.Environment.OSVersion.Version.ToString());

            _telemetry.Initialize(
                Dsn.Trim(),
                //projectName: "Workshop",
                environment: string.IsNullOrWhiteSpace(Environment) ? "development" : Environment.Trim(),
                isDebug: true,
                contextData: contextData);

            _isInitialized = true;
            StatusMessage = $"Initialized (environment: {(string.IsNullOrWhiteSpace(Environment) ? "development" : Environment.Trim())}).";
            StatusSeverity = InfoBarSeverity.Success;
            AppendActivity("Initialized Sentry. Auto session started; a test breadcrumb has been queued.");

            // Persist the working DSN/environment so they are restored next session.
            LocalSettingsHelper.SaveSetting(DsnSettingKey, Dsn.Trim());
            LocalSettingsHelper.SaveSetting(
                EnvironmentSettingKey,
                string.IsNullOrWhiteSpace(Environment) ? "development" : Environment.Trim());
            AppendActivity(RuntimePackageHelper.IsMSIX
                ? "Saved DSN for the next session."
                : "DSN not persisted (settings require a packaged/MSIX build).");

            // First event so the session/release show up quickly.
            _telemetry.LogInformation(typeof(SentryPage), message: "Sentry test page initialized the service.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Initialization failed: {ex.Message}";
            StatusSeverity = InfoBarSeverity.Error;
            AppendActivity($"[error] Initialize threw: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void LogAndTrackErrorTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        var ex = CreateSampleException("Handled error captured via LogAndTrackError extension.");
        _telemetry!.LogAndTrackError(typeof(SentryPage), ex, [("path", "log-and-track")]);
        AppendActivity($"Logged + tracked error: {ex.GetType().Name}.");
    }

    private void LogInformationTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        _telemetry!.LogInformation(typeof(SentryPage), message: "Information breadcrumb + structured log from the test page.");
        AppendActivity("Logged information (breadcrumb + structured log).");
    }

    private void LogNavigationTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        _telemetry!.LogNavigation("HomePage", "SentryPage", typeof(SentryPage));
        AppendActivity("Logged navigation breadcrumb (HomePage -> SentryPage).");
    }

    private void LogWarningTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        _telemetry!.Log(
            "Warning with custom tags from the test page.",
            Microsoft.Extensions.Logging.LogLevel.Warning,
            callerType: typeof(SentryPage),
            tags: [("feature", "sentry-test"), ("severity", "warning")]);
        AppendActivity("Logged warning with tags (feature, severity).");
    }

    private void ReportUnhandledTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        var ex = CreateSampleException("Simulated unhandled exception from the test page.");
        var id = _telemetry!.ReportUnhandledException(ex);
        AppendActivity($"Reported fatal unhandled exception (event id: {id}). Flushed.");
    }

    private void StartChildTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        if (string.IsNullOrEmpty(_transactionId))
        {
            AppendActivity("[skipped] Start a transaction before adding a child span.");
            return;
        }
        _childId = Guid.NewGuid().ToString();
        _telemetry!.StartTrace(
            _childId,
            name: "Child work",
            operation: "workshop.child",
            parentId: _transactionId,
            data: [("kind", "child-span")]);
        AppendActivity($"Started child span (id: {_childId[..8]}) under transaction.");
    }

    private void StartTraceTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        _transactionId = Guid.NewGuid().ToString();
        _telemetry!.StartTrace(
            _transactionId,
            name: "Test transaction",
            operation: "workshop.test",
            data: [("started-from", "test-page")]);
        AppendActivity($"Started transaction (id: {_transactionId[..8]}).");
    }

    private void TrackErrorTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!EnsureReady())
        {
            return;
        }
        var ex = CreateSampleException("Handled error captured via TrackError.");
        _telemetry!.TrackError(ex, [("handled", "true"), ("source", "test-page")], "Handled error from the test page.");
        AppendActivity($"Tracked handled error: {ex.GetType().Name}.");
    }
}