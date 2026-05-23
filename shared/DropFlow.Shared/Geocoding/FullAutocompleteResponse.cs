namespace DropFlow.Shared.Geocoding;

public class FullAutocompleteResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Step1_Input { get; set; } = string.Empty;
    public int Step2_SuggestionsCount { get; set; }
    public List<AddressSuggestion> Step2_Suggestions { get; set; } = new();
    public AddressSuggestion? Step3_SelectedSuggestion { get; set; }
    public AddressDetails? Step4_AddressDetails { get; set; }
}