using System.Windows;

namespace SmartHomeUI.Views;

public partial class EnterPatDialog : Window
{
    public string? Pat { get; private set; }

    public EnterPatDialog()
    {
        InitializeComponent();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Pat = PatBox.Password;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
