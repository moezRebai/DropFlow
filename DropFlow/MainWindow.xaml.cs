using System.Windows;

namespace DropFlow;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(ViewModels.MainViewModel vm)
    {
        InitializeComponent();
        SidebarColumn.Width = new GridLength(60);
        DataContext = vm;
    }
    
    private bool _isSidebarCollapsed;

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        SidebarColumn.Width = _isSidebarCollapsed ? new GridLength(220) :
            new GridLength(60);

        _isSidebarCollapsed = !_isSidebarCollapsed;
    }
}