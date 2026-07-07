namespace backend_dotnet.Modules.Documents;

public static class DocumentAccessLevel
{
    // Cap tai lieu chi admin doc duoc.
    public const string Admin = "admin";

    // Cap tai lieu admin va employee doc duoc.
    public const string Employee = "employee";

    // Cap tai lieu admin, employee va guest doc duoc.
    public const string Guest = "guest";
}
