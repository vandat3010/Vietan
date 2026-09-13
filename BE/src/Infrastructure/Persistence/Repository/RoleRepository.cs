using Backend.Application.Interfaces.Repositories;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repository;

public class RoleRepository(ApplicationDbContext context) : GenericRepository<Role>(context), IRoleRepository
{
    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        await Context.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        await Context.Roles.Where(r => ids.Contains(r.Id)).ToListAsync(cancellationToken);
}
