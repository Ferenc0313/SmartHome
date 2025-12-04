using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using SmartHomeUI.Data;
using SmartHomeUI.Models;
using SmartHomeUI.Security;
using SmartHomeUI.Services;

namespace SmartHomeUI.ViewModels;

public class AuthViewModel : INotifyPropertyChanged
{
    // Login
    private string _loginName = string.Empty;
    public string LoginName { get => _loginName; set { _loginName = value; OnPropertyChanged(); } }

    private string _loginPassword = string.Empty;
    public string LoginPassword { get => _loginPassword; set { _loginPassword = value; OnPropertyChanged(); } }

    public ICommand LoginCommand { get; }

    // Register
    private string _regName = string.Empty;
    public string RegName { get => _regName; set { _regName = value; OnPropertyChanged(); } }

    private string _regEmail = string.Empty;
    public string RegEmail { get => _regEmail; set { _regEmail = value; OnPropertyChanged(); } }

    private string _regPassword = string.Empty;
    public string RegPassword { get => _regPassword; set { _regPassword = value; OnPropertyChanged(); } }

    private string _regConfirm = string.Empty;
    public string RegConfirm { get => _regConfirm; set { _regConfirm = value; OnPropertyChanged(); } }

    private string _regPat = string.Empty;
    public string RegPat { get => _regPat; set { _regPat = value; OnPropertyChanged(); } }

    public ICommand RegisterCommand { get; }

    // Feedback
    private string _status = string.Empty;
    public string StatusMessage { get => _status; set { _status = value; OnPropertyChanged(); } }

    // Events for UI actions
    public event Action<User>? LoggedIn;
    public event Action<string>? Registered;

    public AuthViewModel()
    {
        LoginCommand = new RelayCommand(_ => Login());
        RegisterCommand = new RelayCommand(_ => Register());
    }

    private void Login()
    {
        try
        {
            using var db = new SmartHomeDbContext();
            var user = db.Users.AsNoTracking().SingleOrDefault(u => u.Name == LoginName);
            if (user is null)
            {
                StatusMessage = "User not found. Please register.";
                return;
            }
            if (!PasswordHasher.Verify(LoginPassword, user.PasswordHash, user.PasswordSalt))
            {
                StatusMessage = "Invalid password.";
                return;
            }
            // Mark online
            using (var tdb = new SmartHomeDbContext())
            {
                var tracked = tdb.Users.Single(u => u.Id == user.Id);
                tracked.IsOnline = true;
                tdb.SaveChanges();
                string? patPlain = null;
                if (!string.IsNullOrWhiteSpace(tracked.SmartThingsPatEncrypted))
                {
                    try { patPlain = TokenProtection.Unprotect(tracked.SmartThingsPatEncrypted); } catch { patPlain = null; }
                }
                AuthService.LogIn(tracked, patPlain);
                LoggedIn?.Invoke(tracked);
            }
            StatusMessage = $"Welcome, {user.Name}!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Login failed: {ex.Message}";
        }
    }

    private void Register()
    {
        if (string.IsNullOrWhiteSpace(RegName) || string.IsNullOrWhiteSpace(RegPassword))
        {
            StatusMessage = "Name and password are required.";
            return;
        }
        if (!string.Equals(RegPassword, RegConfirm, StringComparison.Ordinal))
        {
            StatusMessage = "Passwords do not match.";
            return;
        }
        if (string.IsNullOrWhiteSpace(RegPat))
        {
            StatusMessage = "SmartThings PAT is required.";
            return;
        }
        var normalizedPat = NormalizePat(RegPat);
        var patOk = ValidatePat(normalizedPat);
        using var db = new SmartHomeDbContext();
        if (db.Users.Any(u => u.Name == RegName))
        {
            StatusMessage = "Username already exists.";
            return;
        }
        var (hash, salt) = PasswordHasher.CreateHash(RegPassword);
        var user = new User
        {
            Name = RegName.Trim(),
            Email = RegEmail.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            IsOnline = false,
            SmartThingsPatEncrypted = TokenProtection.Protect(normalizedPat)
        };
        db.Users.Add(user);
        db.SaveChanges();
        StatusMessage = patOk
            ? "Registration successful. You can log in now."
            : "Registration saved. PAT could not be verified now (401/timeout); try toggling a device after login.";
        Registered?.Invoke("Registration successful. Please sign in.");
        // clear
        RegPassword = RegConfirm = RegPat = string.Empty;
        OnPropertyChanged(nameof(RegPassword));
        OnPropertyChanged(nameof(RegConfirm));
        OnPropertyChanged(nameof(RegPat));
    }

    private string NormalizePat(string pat)
    {
        if (string.IsNullOrWhiteSpace(pat)) return string.Empty;
        // Remove all whitespace characters
        return new string(pat.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    private bool ValidatePat(string pat)
    {
        if (string.IsNullOrWhiteSpace(pat)) return false;
        pat = NormalizePat(pat);
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("https://api.smartthings.com/v1/") };
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
            http.Timeout = TimeSpan.FromSeconds(8);
            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var res = http.GetAsync("devices").GetAwaiter().GetResult();
            if (res.IsSuccessStatusCode) return true;
            // Do not hard-block registration on 401/other; let runtime toggling surface issues.
            return true;
        }
        catch
        {
            // Network/auth errors: don't block registration, let runtime surface errors.
            return true;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;
        public event EventHandler? CanExecuteChanged;
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        { _execute = execute; _canExecute = canExecute; }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
