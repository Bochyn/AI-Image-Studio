using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;

namespace RhinoImageStudio.Backend.Validation;

public static class ProjectValidator
{
    public static async Task<string?> EnsureExistsAsync(AppDbContext db, Guid projectId, CancellationToken ct)
    {
        var exists = await db.Projects.AsNoTracking().AnyAsync(p => p.Id == projectId, ct);
        return exists ? null : "Project not found";
    }
}
