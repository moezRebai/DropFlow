using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DropFlow.ViewModels;

public partial class DeliveryItemViewModel : ObservableObject
{
    public Guid Id { get; set; }
    public string ClientName { get; set; } = "";
    public string StoreName { get; set; } = "";
    public DateTime Date { get; set; }
    public string Status { get; set; } = "";
    public bool IsAddNewCard { get; set; }

    // ✨ NOUVELLES PROPRIÉTÉS POUR LES IMAGES
    public string? ProductImageUrl { get; set; }
    public int ProductCount { get; set; } = 1;

    // computed props
    public string StatusIcon => Status switch
    {
        "Planned" => "ClockOutline",
        "Confirmed" => "CheckCircleOutline",
        "Canceled"  => "CloseCircleOutline",
        "Done"      => "ClipboardCheckOutline",
        _ => "InformationOutline"
    };

    public Brush StatusColor
    {
        get
        {
            var key = Status switch
            {
                "Planned" => "DeliveryStatusPlannedBrush",
                "Confirmed" => "DeliveryStatusConfirmedBrush",
                "Canceled"  => "DeliveryStatusCanceledBrush",
                "Done"      => "DeliveryStatusDoneBrush",
                _ => "DeliveryStatusDefaultBrush"
            };
            return (Brush)Application.Current.FindResource(key);
        }
    }

    [RelayCommand] private void Edit() { /* ... */ }
    [RelayCommand] private void Delete() { /* ... */ }
    [RelayCommand] private void Details() { /* ... */ }
}

// ═══════════════════════════════════════════════════════════════
// HELPER POUR GÉNÉRER DES IMAGES ALÉATOIRES
// ═══════════════════════════════════════════════════════════════