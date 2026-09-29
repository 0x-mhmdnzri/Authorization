using Microsoft.AspNetCore.Identity;
using Authorization.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

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
    public DbSet<AccessRule> AccessRules => Set<AccessRule>();
    public DbSet<RuleRateCounter> RuleRateCounters => Set<RuleRateCounter>();

    // Admin panel / auth foundation
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<MenuSection> MenuSections => Set<MenuSection>();
    public DbSet<SectionPermission> SectionPermissions => Set<SectionPermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.Department).HasMaxLength(100);
            entity.Property(u => u.ClearanceLevel).HasMaxLength(50);
            entity.HasIndex(u => u.IsGod);
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

        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

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

        builder.Entity<ResourcePermission>(entity =>
        {
            entity.ToTable("ResourcePermissions");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.SubjectId).HasMaxLength(450).IsRequired();
            entity.Property(p => p.Permissions).HasMaxLength(100).IsRequired();
            entity.Property(p => p.GrantedById).HasMaxLength(450).IsRequired();
            entity.HasOne(p => p.Resource).WithMany().HasForeignKey(p => p.ResourceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(p => new { p.ResourceId, p.SubjectId }).IsUnique();
        });

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

        builder.Entity<AccessRule>(entity =>
        {
            entity.ToTable("AccessRules");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Effect).HasMaxLength(20).IsRequired();
            entity.Property(r => r.ResourceType).HasMaxLength(100);
            entity.Property(r => r.Action).HasMaxLength(50);
            entity.Property(r => r.SourceIpAllowList).HasMaxLength(500);
            entity.Property(r => r.SourceIpDenyList).HasMaxLength(500);
            entity.Property(r => r.DaysOfWeek).HasMaxLength(50);
            entity.Property(r => r.RequiredDepartment).HasMaxLength(100);
            entity.Property(r => r.RequiredRole).HasMaxLength(100);
            entity.HasIndex(r => new { r.IsEnabled, r.Priority });
        });

        builder.Entity<RuleRateCounter>(entity =>
        {
            entity.ToTable("RuleRateCounters");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.UserId).HasMaxLength(450).IsRequired();
            entity.HasIndex(c => new { c.UserId, c.RuleId }).IsUnique();
        });

        // Refresh tokens
        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.UserId).HasMaxLength(450).IsRequired();
            entity.Property(t => t.Token).HasMaxLength(500).IsRequired();
            entity.Property(t => t.CreatedByIp).HasMaxLength(100);
            entity.Property(t => t.RevokedByIp).HasMaxLength(100);
            entity.Property(t => t.ReplacedByToken).HasMaxLength(500);
            entity.Property(t => t.ReasonRevoked).HasMaxLength(200);
            entity.HasIndex(t => t.Token).IsUnique();
            entity.HasIndex(t => t.UserId);
            entity.HasOne(t => t.User).WithMany(u => u.RefreshTokens).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Menu sections
        builder.Entity<MenuSection>(entity =>
        {
            entity.ToTable("MenuSections");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Key).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Title).HasMaxLength(200).IsRequired();
            entity.Property(s => s.Description).HasMaxLength(500);
            entity.Property(s => s.Href).HasMaxLength(300);
            entity.Property(s => s.Icon).HasMaxLength(100);
            entity.Property(s => s.AuthorizationMethods).HasMaxLength(200).IsRequired();
            entity.Property(s => s.ParentKey).HasMaxLength(100);
            entity.HasIndex(s => s.Key).IsUnique();
            entity.HasIndex(s => s.SortOrder);
        });

        // Section permissions
        builder.Entity<SectionPermission>(entity =>
        {
            entity.ToTable("SectionPermissions");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.UserId).HasMaxLength(450).IsRequired();
            entity.Property(p => p.GrantedById).HasMaxLength(450).IsRequired();
            entity.HasOne(p => p.MenuSection).WithMany(s => s.Permissions).HasForeignKey(p => p.MenuSectionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.User).WithMany(u => u.SectionPermissions).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(p => new { p.UserId, p.MenuSectionId }).IsUnique();
        });
    }
}
