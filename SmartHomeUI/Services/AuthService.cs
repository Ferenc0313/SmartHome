using SmartHomeUI.Models;

namespace SmartHomeUI.Services;

public static class AuthService
{
    public static User? CurrentUser { get; private set; }
    public static string? CurrentSmartThingsPat { get; private set; }

    public static void LogIn(User user, string? decryptedSmartThingsPat = null)
    {
        CurrentUser = user;
        CurrentSmartThingsPat = decryptedSmartThingsPat;
    }

    public static void LogOut()
    {
        CurrentUser = null;
        CurrentSmartThingsPat = null;
    }
}
