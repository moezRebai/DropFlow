namespace DropFlow.Shared.Geocoding;

public class AutocompleteResponse
{
    public bool Success { get; set; }
    public string Input { get; set; } = string.Empty;
    public int SuggestionsCount { get; set; }
    public List<AddressSuggestion> Suggestions { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}