using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations;

/// <summary>
/// Baselines SCADA/Timescale entities into the EF model. Tables under schemas
/// <c>scada</c> and <c>history</c> already exist in the database (created outside EF),
/// so <see cref="Up"/> intentionally does not recreate them.
/// </summary>
public partial class AddScadaEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // No-op: scada.* and history.* already present.
        // This migration only updates ApplicationDbContextModelSnapshot.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No-op: do not drop live SCADA/history tables on rollback.
    }
}
