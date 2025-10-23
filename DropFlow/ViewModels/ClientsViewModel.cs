using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace DropFlow.ViewModels;

public partial class ClientsViewModel : ObservableObject
{
    public IEnumerable<ISeries> Series { get; set; }
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
    

    public ClientsViewModel()
    {
        var months = new[]
        {
            "Oct", "Nov", "Dec", "Jan", "Feb", "Mar",
            "Apr", "May", "Jun", "Jul", "Aug", "Sep"
        };

        double[] revenues = { 120, 130, 140, 110, 125, 135, 150, 155, 160, 158, 145, 150 };
        double[] deliveries = { 10, 34, 36, 31, 33, 35, 40, 41, 43, 42, 39, 40 };

        Series = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Revenue (k€)",
                Values = revenues
            },
            new ColumnSeries<double>
            {
                Name = "Deliveries",
                Values = deliveries
            }
        };

        XAxes = new Axis[]
        {
            new Axis
            {
                Labels = months,
                LabelsRotation = 0
            }
        };

        YAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = 0
            }
        };
    }
}
