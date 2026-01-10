using System;
using System.Windows;
using System.Windows.Controls;

namespace SmartHomeUI.Presentation.Behaviors;

public static class PersistedExpander
{
    public static readonly DependencyProperty KeyProperty =
        DependencyProperty.RegisterAttached(
            "Key",
            typeof(string),
            typeof(PersistedExpander),
            new PropertyMetadata(null, OnKeyChanged));

    public static void SetKey(DependencyObject element, string value) => element.SetValue(KeyProperty, value);
    public static string GetKey(DependencyObject element) => (string)element.GetValue(KeyProperty);

    public static readonly DependencyProperty DefaultExpandedProperty =
        DependencyProperty.RegisterAttached(
            "DefaultExpanded",
            typeof(bool),
            typeof(PersistedExpander),
            new PropertyMetadata(true));

    public static void SetDefaultExpanded(DependencyObject element, bool value) => element.SetValue(DefaultExpandedProperty, value);
    public static bool GetDefaultExpanded(DependencyObject element) => (bool)element.GetValue(DefaultExpandedProperty);

    private static void OnKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Expander expander) return;

        expander.Loaded -= ExpanderOnLoaded;
        expander.Expanded -= ExpanderOnExpanded;
        expander.Collapsed -= ExpanderOnCollapsed;

        if (e.NewValue is string key && !string.IsNullOrWhiteSpace(key))
        {
            expander.Loaded += ExpanderOnLoaded;
            expander.Expanded += ExpanderOnExpanded;
            expander.Collapsed += ExpanderOnCollapsed;
        }
    }

    private static void ExpanderOnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Expander expander) return;
        var key = GetKey(expander);
        if (string.IsNullOrWhiteSpace(key)) return;

        var store = Application.Current?.Properties;
        if (store is null)
        {
            expander.IsExpanded = GetDefaultExpanded(expander);
            return;
        }

        var storeKey = BuildStoreKey(key);
        if (store.Contains(storeKey) && store[storeKey] is bool b)
            expander.IsExpanded = b;
        else
            expander.IsExpanded = GetDefaultExpanded(expander);
    }

    private static void ExpanderOnExpanded(object sender, RoutedEventArgs e) => Persist(sender, isExpanded: true);
    private static void ExpanderOnCollapsed(object sender, RoutedEventArgs e) => Persist(sender, isExpanded: false);

    private static void Persist(object sender, bool isExpanded)
    {
        if (sender is not Expander expander) return;
        var key = GetKey(expander);
        if (string.IsNullOrWhiteSpace(key)) return;

        var store = Application.Current?.Properties;
        if (store is null) return;
        store[BuildStoreKey(key)] = isExpanded;
    }

    private static string BuildStoreKey(string key) => $"PersistedExpander:{key}";
}
