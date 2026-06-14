using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SpatialLabsOptimizer.Controls;

public sealed partial class ShellActivityInfoBar : UserControl
{
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(ShellActivityInfoBar),
            new PropertyMetadata(false, OnIsOpenChanged));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(ShellActivityInfoBar),
            new PropertyMetadata(string.Empty, OnMessageChanged));

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double), typeof(ShellActivityInfoBar),
            new PropertyMetadata(0d, OnProgressChanged));

    public ShellActivityInfoBar()
    {
        InitializeComponent();
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ShellActivityInfoBar bar)
        {
            bar.ActivityInfoBar.IsOpen = (bool)e.NewValue;
        }
    }

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ShellActivityInfoBar bar)
        {
            bar.MessageBlock.Text = e.NewValue as string ?? "";
        }
    }

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ShellActivityInfoBar bar)
        {
            var value = (double)e.NewValue;
            bar.ActivityProgressBar.IsIndeterminate = value <= 0;
            bar.ActivityProgressBar.Value = value;
        }
    }
}
