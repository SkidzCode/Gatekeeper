namespace GateKeeper.Server.Models.Resources;

public class UpdateResourceEntryRequest
{
    public string Value { get; set; }
    public string Type { get; set; }
    public string Comment { get; set; } = string.Empty;
}