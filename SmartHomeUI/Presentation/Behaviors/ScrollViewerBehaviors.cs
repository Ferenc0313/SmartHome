using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SmartHomeUI.Presentation.Behaviors;

public static class ScrollViewerBehaviors
{
    public static readonly DependencyProperty ScrollToTopOnLoadedProperty =
        DependencyProperty.RegisterAttached(
            "ScrollToTopOnLoaded",
            typeof(bool),
            typeof(ScrollViewerBehaviors),
            new PropertyMetadata(false, OnScrollToTopOnLoadedChanged));

    public static void SetScrollToTopOnLoaded(DependencyObject element, bool value) =>
        element.SetValue(ScrollToTopOnLoadedProperty, value);

    public static bool GetScrollToTopOnLoaded(DependencyObject element) =>
        (bool)element.GetValue(ScrollToTopOnLoadedProperty);

    private static void OnScrollToTopOnLoadedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer sv) return;

        sv.Loaded -= OnLoaded;
        if (e.NewValue is true)
        {
            sv.Loaded += OnLoaded;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;

        // Wait for layout/items generation to avoid "jumping" after initial render.
        sv.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => sv.ScrollToTop()));
    }
}

