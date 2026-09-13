namespace Backend.Shared.Models;

/// <summary>
/// Minimal id/name pair for dropdown/select-list style endpoints
/// (e.g. "GET /api/v1/roles/lookup"). Deliberately generic so every module
/// (Users, Roles, Warehouses, Products, ...) can reuse it instead of each
/// defining its own "XxxLookupDto" with the same two fields.
/// </summary>
public class LookupItem(Guid id, string name)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
}
