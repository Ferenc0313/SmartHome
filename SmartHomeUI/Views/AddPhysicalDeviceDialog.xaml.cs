using System.Collections.Generic;
using System.Windows;
using SmartHomeUI.Models;
using SmartHomeUI.ViewModels;

namespace SmartHomeUI.Views;

public partial class AddPhysicalDeviceDialog : Window
{
    public AddPhysicalDeviceDialogViewModel ViewModel { get; }

    public IReadOnlyList<PhysicalDevice> SelectedDevices { get; private set; } = new List<PhysicalDevice>();

    public AddPhysicalDeviceDialog(AddPhysicalDeviceDialogViewModel vm)
    {
        InitializeComponent();
        ViewModel = vm;
        DataContext = ViewModel;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        SelectedDevices = ViewModel.GetSelectedDevices();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
