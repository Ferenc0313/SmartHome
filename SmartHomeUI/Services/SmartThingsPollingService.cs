using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartHomeUI.Services;

/// <summary>
/// Periodically pulls state from SmartThings and applies it to the local device state.
/// </summary>
public sealed class SmartThingsPollingService : ISmartThingsPollingService
{
    private readonly SmartThingsPollingOptions _options;
    private readonly ILogger<SmartThingsPollingService> _logger;
    private readonly Random _random = new();
    private CancellationTokenSource? _cts;
    private Task? _runner;
    private DateTime? _resumeAtUtc;

    public SmartThingsPollingService(
        SmartThingsPollingOptions? options,
        ILogger<SmartThingsPollingService> logger)
    {
        _options = options ?? new SmartThingsPollingOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken token = default)
    {
        if (_runner != null && !_runner.IsCompleted) return Task.CompletedTask;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        _runner = Task.Run(() => RunAsync(_cts.Token));
        _logger.LogInformation("SmartThings polling started with interval {Interval}s", _options.Interval.TotalSeconds);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;
        _cts.Cancel();
        if (_runner is not null)
        {
            try { await _runner.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
        _cts.Dispose();
        _cts = null;
        _runner = null;
        _logger.LogInformation("SmartThings polling stopped");
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var delay = _options.Interval;
            try
            {
                delay = await PollOnceAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SmartThings polling iteration failed");
            }

            var jitterMs = _options.JitterMilliseconds;
            if (jitterMs > 0)
            {
                var delta = _random.Next(-jitterMs, jitterMs);
                delay = delay + TimeSpan.FromMilliseconds(delta);
            }
            if (delay < TimeSpan.FromSeconds(1)) delay = TimeSpan.FromSeconds(1);

            try
            {
                await Task.Delay(delay, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<TimeSpan> PollOnceAsync(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        if (_resumeAtUtc.HasValue && now < _resumeAtUtc.Value)
        {
            var wait = _resumeAtUtc.Value - now;
            _logger.LogWarning("SmartThings polling paused for {Seconds}s after repeated auth errors", wait.TotalSeconds);
            return wait;
        }

        var pat = DeviceService.NormalizePat(AuthService.CurrentSmartThingsPat ?? Environment.GetEnvironmentVariable("SMARTTHINGS_PAT"));
        if (string.IsNullOrWhiteSpace(pat))
        {
            _logger.LogDebug("Skipping SmartThings poll: PAT missing");
            return _options.Interval;
        }

        var devices = DeviceService.Devices.Where(d => d.IsPhysical && !string.IsNullOrWhiteSpace(d.PhysicalDeviceId)).ToList();
        if (devices.Count == 0)
        {
            _logger.LogDebug("Skipping SmartThings poll: no physical devices");
            return _options.Interval;
        }

        _logger.LogDebug("Polling SmartThings for {Count} device(s)", devices.Count);
        var client = new SmartThingsClient(new HttpClient(), pat);

        foreach (var dev in devices)
        {
            token.ThrowIfCancellationRequested();
            var result = await client.GetSwitchStateAsync(dev.PhysicalDeviceId!, token).ConfigureAwait(false);
            if (result.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = result.RetryAfter ?? TimeSpan.FromSeconds(60);
                _logger.LogWarning("SmartThings rate limit hit for device {DeviceId}. Retry after {RetryAfter}s", dev.PhysicalDeviceId, retryAfter.TotalSeconds);
                return retryAfter;
            }

            if (result.State is null)
            {
                _logger.LogWarning("SmartThings status fetch failed for device {DeviceId} (HTTP {Status})", dev.PhysicalDeviceId, (int)result.StatusCode);
                if (result.StatusCode == HttpStatusCode.Unauthorized || result.StatusCode == HttpStatusCode.Forbidden)
                {
                    _resumeAtUtc = DateTime.UtcNow.AddMinutes(5);
                }
                continue;
            }

            var targetOn = result.State.Equals("on", StringComparison.OrdinalIgnoreCase);
            DeviceService.UpdateStateFromExternal(dev.Id, targetOn);
        }

        return _options.Interval;
    }
}
