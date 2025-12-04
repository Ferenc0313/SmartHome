using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartHomeUI.Services;

/// <summary>
/// Sends on/off commands to SmartThings devices.
/// </summary>
public static class SmartThingsSwitchService
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.smartthings.com/v1/")
    };

    public static bool SetSwitchState(string pat, string deviceId, bool turnOn)
    {
        try
        {
            var task = SetSwitchStateAsync(pat, deviceId, turnOn);
            task.Wait();
            return task.Result;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> SetSwitchStateAsync(string pat, string deviceId, bool turnOn)
    {
        if (string.IsNullOrWhiteSpace(pat)) throw new ArgumentException("PAT is required.", nameof(pat));
        pat = new string(pat.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("deviceId is required.", nameof(deviceId));

        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        var cmd = new
        {
            commands = new[]
            {
                new { component = "main", capability = "switch", command = turnOn ? "on" : "off" }
            }
        };
        var json = JsonSerializer.Serialize(cmd);
        using var res = await Http.PostAsync($"devices/{deviceId}/commands", new StringContent(json, Encoding.UTF8, "application/json")).ConfigureAwait(false);
        return res.IsSuccessStatusCode;
    }
}
