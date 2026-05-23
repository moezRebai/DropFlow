using DropFlow.Mobile.Views;

namespace DropFlow.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("delivery", typeof(DeliveryDetailPage));
        Routing.RegisterRoute("delivery/validation", typeof(ValidationPage));
        Routing.RegisterRoute("history", typeof(HistoryPage));
        Routing.RegisterRoute("profile", typeof(ProfilePage));
    }
}
