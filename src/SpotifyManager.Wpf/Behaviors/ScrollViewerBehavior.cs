using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpotifyManager.Wpf.Behaviors;

public static class ScrollViewerBehavior
{
    public static readonly DependencyProperty EnableMouseWheelProperty =
        DependencyProperty.RegisterAttached(
            "EnableMouseWheel",
            typeof(bool),
            typeof(ScrollViewerBehavior),
            new PropertyMetadata(false, OnEnableMouseWheelChanged));

    public static bool GetEnableMouseWheel(DependencyObject obj)
    {
        return (bool)obj.GetValue(EnableMouseWheelProperty);
    }

    public static void SetEnableMouseWheel(DependencyObject obj, bool value)
    {
        obj.SetValue(EnableMouseWheelProperty, value);
    }

    private static void OnEnableMouseWheelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if ((bool)e.NewValue)
            {
                element.PreviewMouseWheel += Element_PreviewMouseWheel;
            }
            else
            {
                element.PreviewMouseWheel -= Element_PreviewMouseWheel;
            }
        }
    }

    private static void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is UIElement element)
        {
            // 親のScrollViewerを探す
            var scrollViewer = FindParent<ScrollViewer>(element);
            if (scrollViewer != null)
            {
                // マウスホイールのスクロール量を調整（3行分）
                var scrollAmount = e.Delta > 0 ? -60 : 60;
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + scrollAmount);
                e.Handled = true;
            }
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
        
        if (parent == null)
            return null;

        if (parent is T parentAsT)
            return parentAsT;

        return FindParent<T>(parent);
    }
}