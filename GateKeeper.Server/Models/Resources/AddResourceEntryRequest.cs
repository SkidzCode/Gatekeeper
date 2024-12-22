namespace GateKeeper.Server.Models.Resources;

public class AddResourceEntryRequest
{
    public string Key { get; set; }
    public string Value { get; set; }
    public string Type { get; set; }
    public string Comment { get; set; }
}