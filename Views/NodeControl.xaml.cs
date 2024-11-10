using System.Windows.Controls;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RenPy_VisualScripting.Views;

public partial class NodeControl : UserControl
{
    public NodeControl()
    {
        InitializeComponent();
    }
}

// Add this converter class
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
