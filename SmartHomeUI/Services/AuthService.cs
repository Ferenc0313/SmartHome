using SmartHomeUI.Models;

namespace SmartHomeUI.Services;

public static class AuthService
{
    public static User? CurrentUser { get; private set; }

    public static void LogIn(User user) => CurrentUser = user;
    public static void LogOut() => CurrentUser = null;
}

