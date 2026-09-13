namespace Backend.Domain.Common;

/// <summary>
/// Implemented by entities that should be "deleted" via a flag instead of a real
/// DELETE statement. ApplicationDbContext applies a global query filter
/// (<c>IsDeleted == false</c>) for every type that implements this interface.
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedDate { get; set; }
    string? DeletedBy { get; set; }
}
