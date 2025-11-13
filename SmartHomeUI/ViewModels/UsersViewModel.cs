using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using SmartHomeUI.Data;
using SmartHomeUI.Models;

namespace SmartHomeUI.ViewModels;

public class UsersViewModel : INotifyPropertyChanged
{
    public ObservableCollection<User> Users { get; } = new();

    private User? _selectedUser;
    public User? SelectedUser
    {
        get => _selectedUser;
        set { _selectedUser = value; OnPropertyChanged(); }
    }

    private string _newUserName = string.Empty;
    public string NewUserName
    {
        get => _newUserName;
        set { _newUserName = value; OnPropertyChanged(); (AddUserCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
    }

    public ICommand LoadUsersCommand { get; }
    public ICommand AddUserCommand { get; }
    public ICommand DeleteUserCommand { get; }

    public UsersViewModel()
    {
        LoadUsersCommand = new RelayCommand(_ => LoadUsers());
        AddUserCommand = new RelayCommand(_ => AddUser(), _ => !string.IsNullOrWhiteSpace(NewUserName));
        DeleteUserCommand = new RelayCommand(u => DeleteUser(u as User), u => u is User);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void LoadUsers()
    {
        using var db = new SmartHomeDbContext();
        var items = db.Users.AsNoTracking().OrderBy(u => u.Name).ToList();
        Users.Clear();
        foreach (var u in items)
            Users.Add(u);
    }

    private void AddUser()
    {
        var user = new User { Name = NewUserName.Trim() };
        using var db = new SmartHomeDbContext();
        db.Users.Add(user);
        db.SaveChanges();

        Users.Add(user);
        NewUserName = string.Empty;
        (AddUserCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void DeleteUser(User? user)
    {
        if (user == null) return;
        using var db = new SmartHomeDbContext();
        var entity = new User { Id = user.Id };
        db.Attach(entity);
        db.Remove(entity);
        db.SaveChanges();
        Users.Remove(user);
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;
        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
