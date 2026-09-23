using Microsoft.AspNetCore.Identity;
using Authorization.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Data;

/// <summary>
/// EF Core DbContext using ASP.NET Core Identity tables.
/// IdentityServer / Duende can share the same Identity stores if needed later.
/// Custom tables for advanced authorization models will be added incrementally.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Future: custom tables for ABAC attributes, ReBAC relations, policies, etc.
    // public DbSet<Permission> Permissions { get; set; }
    // public DbSet<RolePermission> RolePermissions { get; set; }
    // public DbSet<Resource> Resources { get; set; }
    // public DbSet<RelationTuple> RelationTuples { get; set; } // for ReBAC

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Customize Identity table names if desired (optional)
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.Department).HasMaxLength(100);
            entity.Property(u => u.ClearanceLevel).HasMaxLength(50);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(r => r.Description).HasMaxLength(256);
            entity.HasOne(r => r.ParentRole)
                  .WithMany()
                  .HasForeignKey(r => r.ParentRoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Keep other Identity tables with default names or customize:
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
    }
}
