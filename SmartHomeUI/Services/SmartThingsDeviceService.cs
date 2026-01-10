using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SmartHomeUI.Models;

namespace SmartHomeUI.Services;


public sealed class SmartThingsDeviceService
{
    private readonly HttpClient _http;

    public SmartThingsDeviceService(HttpClient httpClient)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _http.BaseAddress ??= new Uri("https://api.smartthings.com/v1/");
    }

    public async Task<IReadOnlyList<SmartThingsDeviceDto>> ListDevicesAsync(string pat)
    {
        if (string.IsNullOrWhiteSpace(pat))
            throw new ArgumentException("PAT is required.", nameof(pat));
        pat = new string(pat.Where(c => !char.IsWhiteSpace(c)).ToArray());

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        using var res = await _http.GetAsync("devices").ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"SmartThings device list failed: {(int)res.StatusCode} {res.ReasonPhrase}");
        }

        var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
        var parsed = JsonSerializer.Deserialize<DeviceListResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var items = parsed?.Items ?? new List<DeviceRaw>();
        return items.Select(Map).ToList();
    }

    private SmartThingsDeviceDto Map(DeviceRaw raw)
    {
        var caps = raw.Components?
            .SelectMany(c => c.Capabilities ?? Enumerable.Empty<CapabilityRef>())
            .Select(c => c.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        return new SmartThingsDeviceDto
        {
            DeviceId = raw.DeviceId ?? string.Empty,
            Name = raw.Name ?? string.Empty,
            Label = raw.Label ?? string.Empty,
            OcfDeviceType = raw.Ocf?.DeviceType ?? string.Empty,
            Capabilities = caps
        };
    }

    private sealed class DeviceListResponse
    {
        [JsonPropertyName("items")]
        public List<DeviceRaw> Items { get; set; } = new();
    }

    private sealed class DeviceRaw
    {
        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("ocf")]
        public OcfBlock? Ocf { get; set; }

        [JsonPropertyName("components")]
        public List<ComponentRaw>? Components { get; set; }
    }

    private sealed class OcfBlock
    {
        [JsonPropertyName("deviceType")]
        public string? DeviceType { get; set; }
    }

    private sealed class ComponentRaw
    {
        [JsonPropertyName("capabilities")]
        public List<CapabilityRef>? Capabilities { get; set; }
    }

    private sealed class CapabilityRef
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
