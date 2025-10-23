namespace DropFlow;

public class NavigationItem
{
    public string Title { get; set; }
    public string Icon { get; set; } // MaterialDesign PackIcon Kind
    public string ViewKey { get; set; } // Key to identify which View to load
    public Type ViewModelType { get; set; }
}