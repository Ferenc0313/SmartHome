using System;
using System.Security.Cryptography;
using System.Text;

namespace SmartHomeUI.Services;

/// <summary>
/// Protects/unprotects sensitive tokens using DPAPI (CurrentUser scope).
/// </summary>
public static class TokenProtection
{
    public static string Protect(string plain)
    {
        if (string.IsNullOrWhiteSpace(plain)) throw new ArgumentException("Value is required.", nameof(plain));
        var bytes = Encoding.UTF8.GetBytes(plain);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string protectedBase64)
    {
        if (string.IsNullOrWhiteSpace(protectedBase64)) throw new ArgumentException("Value is required.", nameof(protectedBase64));
        var bytes = Convert.FromBase64String(protectedBase64);
        var plainBytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
