using System.Windows;
using System.Windows.Controls;
using DropFlow.ViewModels;

namespace DropFlow.Views;

public class DeliveryTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DefaultTemplate { get; set; }
    public DataTemplate? AddNewTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is DeliveryItemViewModel vm && vm.IsAddNewCard)
            return AddNewTemplate!;
        return DefaultTemplate!;
    }
}