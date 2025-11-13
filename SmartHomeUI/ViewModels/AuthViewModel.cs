using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
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
                AuthService.LogIn(tracked);
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
            IsOnline = false
        };
        db.Users.Add(user);
        db.SaveChanges();
        StatusMessage = "Registration successful. You can log in now.";
        Registered?.Invoke("Registration successful. Please sign in.");
        // clear
        RegPassword = RegConfirm = string.Empty;
        OnPropertyChanged(nameof(RegPassword));
        OnPropertyChanged(nameof(RegConfirm));
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
