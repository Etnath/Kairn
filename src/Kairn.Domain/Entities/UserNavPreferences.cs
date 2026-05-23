namespace Kairn.Domain.Entities;

public class UserNavPreferences
{
    public string UserId         { get; set; } = "";
    public string CollapsedGroups { get; set; } = ""; // semicolon-separated group keys
}
