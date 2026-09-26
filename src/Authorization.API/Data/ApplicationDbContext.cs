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

    // ABAC tables
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<AbacPolicy> AbacPolicies => Set<AbacPolicy>();
    public DbSet<ResourcePermission> ResourcePermissions => Set<ResourcePermission>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Purpose> Purposes => Set<Purpose>();
    public DbSet<ResourcePurpose> ResourcePurposes => Set<ResourcePurpose>();
    public DbSet<PurposeAccessLog> PurposeAccessLogs => Set<PurposeAccessLog>();
    public DbSet<RiskPolicy> RiskPolicies => Set<RiskPolicy>();
    public DbSet<RiskAssessmentLog> RiskAssessmentLogs => Set<RiskAssessmentLog>();
    public DbSet<RelationTuple> RelationTuples => Set<RelationTuple>();
    public DbSet<PrivilegeDefinition> PrivilegeDefinitions => Set<PrivilegeDefinition>();
    public DbSet<PrivilegeElevationRequest> PrivilegeElevationRequests => Set<PrivilegeElevationRequest>();
    public DbSet<PrivilegedActionLog> PrivilegedActionLogs => Set<PrivilegedActionLog>();
    public DbSet<ContextPolicy> ContextPolicies => Set<ContextPolicy>();
    public DbSet<ContextEvaluationLog> ContextEvaluationLogs => Set<ContextEvaluationLog>();

    // Future: ReBAC, etc.
    // public DbSet<RelationTuple> RelationTuples { get; set; }

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

        // ABAC
        builder.Entity<Resource>(entity =>
        {
            entity.ToTable("Resources");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).HasMaxLength(200).IsRequired();
            entity.Property(r => r.ResourceType).HasMaxLength(100).IsRequired();
            entity.Property(r => r.OwnerDepartment).HasMaxLength(100);
            entity.Property(r => r.Sensitivity).HasMaxLength(50).IsRequired();
            entity.Property(r => r.OwnerId).HasMaxLength(450);
            entity.HasIndex(r => r.ResourceType);
            entity.HasIndex(r => r.OwnerDepartment);
        });

        builder.Entity<AbacPolicy>(entity =>
        {
            entity.ToTable("AbacPolicies");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.ResourceType).HasMaxLength(100);
            entity.Property(p => p.Action).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Effect).HasMaxLength(20).IsRequired();
            entity.Property(p => p.MinimumClearance).HasMaxLength(50);
            entity.Property(p => p.AllowedDepartments).HasMaxLength(500);
            entity.Property(p => p.RequiredSensitivityMax).HasMaxLength(50);
            entity.HasIndex(p => new { p.ResourceType, p.Action, p.IsEnabled });
        });

        // DAC
        builder.Entity<ResourcePermission>(entity =>
        {
            entity.ToTable("ResourcePermissions");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.SubjectId).HasMaxLength(450).IsRequired();
            entity.Property(p => p.Permissions).HasMaxLength(100).IsRequired();
            entity.Property(p => p.GrantedById).HasMaxLength(450).IsRequired();
            entity.HasOne(p => p.Resource)
                  .WithMany()
                  .HasForeignKey(p => p.ResourceId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(p => new { p.ResourceId, p.SubjectId }).IsUnique();
        });


        // PBAC
        builder.Entity<Policy>(entity =>
        {
            entity.ToTable("Policies");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.ResourceType).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Action).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Effect).HasMaxLength(20).IsRequired();
            entity.Property(p => p.RequiredRoles).HasMaxLength(500);
            entity.Property(p => p.RequiredDepartments).HasMaxLength(500);
            entity.Property(p => p.MinimumClearance).HasMaxLength(50);
            entity.Property(p => p.MaxResourceSensitivity).HasMaxLength(50);
            entity.Property(p => p.CreatedBy).HasMaxLength(450);
            entity.HasIndex(p => new { p.ResourceType, p.Action, p.IsEnabled });
        });


        // Purpose-Based Access Control
        builder.Entity<Purpose>(entity =>
        {
            entity.ToTable("Purposes");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Code).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(p => p.Code).IsUnique();
        });

        builder.Entity<ResourcePurpose>(entity =>
        {
            entity.ToTable("ResourcePurposes");
            entity.HasKey(rp => rp.Id);
            entity.HasOne(rp => rp.Resource).WithMany().HasForeignKey(rp => rp.ResourceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(rp => rp.Purpose).WithMany().HasForeignKey(rp => rp.PurposeId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(rp => rp.AllowedRoles).HasMaxLength(500);
            entity.HasIndex(rp => new { rp.ResourceId, rp.PurposeId }).IsUnique();
        });

        builder.Entity<PurposeAccessLog>(entity =>
        {
            entity.ToTable("PurposeAccessLogs");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.UserId).HasMaxLength(450).IsRequired();
            entity.Property(l => l.Action).HasMaxLength(50);
            entity.Property(l => l.Reason).HasMaxLength(500);
            entity.HasIndex(l => new { l.UserId, l.ResourceId, l.AccessedAt });
        });


        // RAdAC
        builder.Entity<RiskPolicy>(entity =>
        {
            entity.ToTable("RiskPolicies");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
        });

        builder.Entity<RiskAssessmentLog>(entity =>
        {
            entity.ToTable("RiskAssessmentLogs");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.UserId).HasMaxLength(450).IsRequired();
            entity.Property(l => l.Action).HasMaxLength(50);
            entity.Property(l => l.Decision).HasMaxLength(50);
            entity.Property(l => l.Reason).HasMaxLength(500);
            entity.HasIndex(l => new { l.UserId, l.AssessedAt });
        });


        // ReBAC
        builder.Entity<RelationTuple>(entity =>
        {
            entity.ToTable("RelationTuples");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.ObjectType).HasMaxLength(100).IsRequired();
            entity.Property(t => t.ObjectId).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Relation).HasMaxLength(100).IsRequired();
            entity.Property(t => t.Subject).HasMaxLength(300).IsRequired();
            entity.Property(t => t.CreatedBy).HasMaxLength(450);
            entity.HasIndex(t => new { t.ObjectType, t.ObjectId, t.Relation, t.Subject }).IsUnique();
            entity.HasIndex(t => new { t.ObjectType, t.ObjectId });
            entity.HasIndex(t => t.Subject);
        });


        // PAC – Privileged Access Control
        builder.Entity<PrivilegeDefinition>(entity =>
        {
            entity.ToTable("PrivilegeDefinitions");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Code).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.AllowedRequesterRoles).HasMaxLength(500);
            entity.Property(p => p.ApproverRoles).HasMaxLength(500);
            entity.HasIndex(p => p.Code).IsUnique();
        });

        builder.Entity<PrivilegeElevationRequest>(entity =>
        {
            entity.ToTable("PrivilegeElevationRequests");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.RequesterId).HasMaxLength(450).IsRequired();
            entity.Property(r => r.Justification).HasMaxLength(1000).IsRequired();
            entity.Property(r => r.ApproverId).HasMaxLength(450);
            entity.Property(r => r.ApproverNote).HasMaxLength(500);
            entity.Property(r => r.Status).HasConversion<int>();
            entity.HasOne(r => r.Privilege).WithMany().HasForeignKey(r => r.PrivilegeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(r => new { r.RequesterId, r.Status });
        });

        builder.Entity<PrivilegedActionLog>(entity =>
        {
            entity.ToTable("PrivilegedActionLogs");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.UserId).HasMaxLength(450).IsRequired();
            entity.Property(l => l.PrivilegeCode).HasMaxLength(100);
            entity.Property(l => l.Action).HasMaxLength(200);
            entity.Property(l => l.Resource).HasMaxLength(300);
            entity.Property(l => l.Details).HasMaxLength(1000);
            entity.HasIndex(l => new { l.UserId, l.PerformedAt });
        });


        // CBAC
        builder.Entity<ContextPolicy>(entity =>
        {
            entity.ToTable("ContextPolicies");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.ResourceType).HasMaxLength(100);
            entity.Property(p => p.Action).HasMaxLength(50);
            entity.Property(p => p.Effect).HasMaxLength(20);
            entity.Property(p => p.AllowedDaysOfWeek).HasMaxLength(50);
            entity.Property(p => p.AllowedCountries).HasMaxLength(200);
            entity.Property(p => p.BlockedCountries).HasMaxLength(200);
            entity.Property(p => p.MinimumAuthMethod).HasMaxLength(50);
            entity.HasIndex(p => new { p.ResourceType, p.Action, p.IsEnabled });
        });

        builder.Entity<ContextEvaluationLog>(entity =>
        {
            entity.ToTable("ContextEvaluationLogs");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.UserId).HasMaxLength(450).IsRequired();
            entity.Property(l => l.Action).HasMaxLength(50);
            entity.Property(l => l.Reason).HasMaxLength(500);
            entity.Property(l => l.MatchedPolicy).HasMaxLength(200);
            entity.Property(l => l.ContextSnapshot).HasMaxLength(1000);
            entity.HasIndex(l => new { l.UserId, l.EvaluatedAt });
        });

    }
}

