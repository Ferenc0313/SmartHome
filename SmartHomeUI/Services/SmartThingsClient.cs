using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace SmartHomeUI.Services;

/// <summary>
/// Minimal SmartThings REST client for switch-capable devices.
/// Uses PAT (Personal Access Token) for bearer authentication.
/// </summary>
public sealed class SmartThingsClient
{
    private static readonly Uri BaseUri = new("https://api.smartthings.com/v1/");
    private readonly HttpClient _http;

    public SmartThingsClient(HttpClient httpClient, string personalAccessToken)
    {
        if (string.IsNullOrWhiteSpace(personalAccessToken))
            throw new ArgumentException("SmartThings PAT is required.", nameof(personalAccessToken));

        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _http.BaseAddress = BaseUri;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
    }

    public Task<HttpResponseMessage> TurnOnAsync(string deviceId) =>
        SendCommandAsync(deviceId, "on");

    public Task<HttpResponseMessage> TurnOffAsync(string deviceId) =>
        SendCommandAsync(deviceId, "off");

    public sealed record SwitchStateResult(string? State, HttpStatusCode StatusCode, TimeSpan? RetryAfter);

    public async Task<SwitchStateResult> GetSwitchStateAsync(string deviceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));

        var res = await _http.GetAsync($"devices/{deviceId}/status", cancellationToken).ConfigureAwait(false);
        if (res.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return new SwitchStateResult(null, res.StatusCode, ReadRetryAfter(res));
        }

        if (!res.IsSuccessStatusCode)
        {
            return new SwitchStateResult(null, res.StatusCode, null);
        }

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
        if (!doc.RootElement.TryGetProperty("components", out var comps) ||
            !comps.TryGetProperty("main", out var main) ||
            !main.TryGetProperty("switch", out var sw) ||
            !sw.TryGetProperty("switch", out var switchObj) ||
            !switchObj.TryGetProperty("value", out var value))
        {
            return new SwitchStateResult(null, res.StatusCode, null);
        }
        return new SwitchStateResult(value.GetString(), res.StatusCode, null);
    }

    private Task<HttpResponseMessage> SendCommandAsync(string deviceId, string command)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command is required.", nameof(command));

        var payload = new
        {
            commands = new[]
            {
                new { component = "main", capability = "switch", command }
            }
        };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return _http.PostAsync($"devices/{deviceId}/commands", content);
    }

    private static TimeSpan? ReadRetryAfter(HttpResponseMessage res)
    {
        if (res.Headers.RetryAfter?.Delta != null)
        {
            return res.Headers.RetryAfter.Delta;
        }
        if (res.Headers.RetryAfter?.Date != null)
        {
            var delta = res.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }
        return null;
    }
}
