using Authorization.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authorization.API.Data;

/// <summary>
/// Seeds GOD user and default admin-panel menu sections + full section grants for GOD/Admin.
/// </summary>
public static class AdminPanelSeed
{
    public static async Task EnsureTablesAsync(ApplicationDbContext context)
    {
        // Idempotent DDL so feature works even before a formal EF migration is generated.
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "RefreshTokens" (
                "Id" uuid PRIMARY KEY,
                "UserId" character varying(450) NOT NULL,
                "Token" character varying(500) NOT NULL,
                "ExpiresAt" timestamp with time zone NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "CreatedByIp" character varying(100),
                "RevokedAt" timestamp with time zone,
                "RevokedByIp" character varying(100),
                "ReplacedByToken" character varying(500),
                "ReasonRevoked" character varying(200),
                CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");
            CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");

            CREATE TABLE IF NOT EXISTS "MenuSections" (
                "Id" uuid PRIMARY KEY,
                "Key" character varying(100) NOT NULL,
                "Title" character varying(200) NOT NULL,
                "Description" character varying(500),
                "Href" character varying(300),
                "Icon" character varying(100),
                "AuthorizationMethods" character varying(200) NOT NULL,
                "SortOrder" integer NOT NULL,
                "IsEnabled" boolean NOT NULL DEFAULT TRUE,
                "ParentKey" character varying(100)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_MenuSections_Key" ON "MenuSections" ("Key");

            CREATE TABLE IF NOT EXISTS "SectionPermissions" (
                "Id" uuid PRIMARY KEY,
                "MenuSectionId" uuid NOT NULL,
                "UserId" character varying(450) NOT NULL,
                "CanRead" boolean NOT NULL,
                "CanWrite" boolean NOT NULL,
                "GrantedById" character varying(450) NOT NULL,
                "GrantedAt" timestamp with time zone NOT NULL,
                "ExpiresAt" timestamp with time zone,
                CONSTRAINT "FK_SectionPermissions_MenuSections" FOREIGN KEY ("MenuSectionId") REFERENCES "MenuSections" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_SectionPermissions_Users" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_SectionPermissions_UserId_MenuSectionId" ON "SectionPermissions" ("UserId", "MenuSectionId");

            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "IsGod" boolean NOT NULL DEFAULT FALSE;
            """);
    }

    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        await EnsureTablesAsync(context);

        // --- Single GOD user ---
        const string godEmail = "god@authorization.local";
        var god = await userManager.FindByEmailAsync(godEmail);
        if (god is null)
        {
            god = new ApplicationUser
            {
                UserName = godEmail,
                Email = godEmail,
                FirstName = "GOD",
                LastName = "System",
                Department = "System",
                ClearanceLevel = "TopSecret",
                EmailConfirmed = true,
                IsActive = true,
                IsGod = true
            };
            var created = await userManager.CreateAsync(god, "God123!");
            if (created.Succeeded)
            {
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = "Admin",
                        Description = "Administrator"
                    });
                }
                await userManager.AddToRoleAsync(god, "Admin");
            }
        }
        else if (!god.IsGod)
        {
            god.IsGod = true;
            await userManager.UpdateAsync(god);
        }

        // Ensure only one GOD
        var extraGods = await userManager.Users.Where(u => u.IsGod && u.Email != godEmail).ToListAsync();
        foreach (var extra in extraGods)
        {
            extra.IsGod = false;
            await userManager.UpdateAsync(extra);
        }

        // Promote existing admin to have Admin role (not GOD)
        var admin = await userManager.FindByEmailAsync("admin@authorization.local");

        // --- Menu sections ---
        if (!await context.MenuSections.AnyAsync())
        {
            var sections = new List<MenuSection>
            {
                new() { Key = "dashboard", Title = "Dashboard", Href = "/dashboard", Icon = "home", AuthorizationMethods = "RBAC", SortOrder = 10 },
                new() { Key = "users", Title = "Users", Href = "/users", Icon = "users", AuthorizationMethods = "RBAC,DAC", SortOrder = 20, Description = "Create users and assign section access" },
                new() { Key = "roles", Title = "Roles & Assignments", Href = "/roles", Icon = "shield", AuthorizationMethods = "RBAC", SortOrder = 30 },
                new() { Key = "resources", Title = "Resources & ACL", Href = "/resources", Icon = "folder", AuthorizationMethods = "DAC,MAC", SortOrder = 40 },
                new() { Key = "abac", Title = "ABAC Policies", Href = "/abac", Icon = "sliders", AuthorizationMethods = "ABAC", SortOrder = 50 },
                new() { Key = "pbac", Title = "Central Policies", Href = "/pbac", Icon = "book", AuthorizationMethods = "PBAC-Policy", SortOrder = 60 },
                new() { Key = "purpose", Title = "Purposes", Href = "/purpose", Icon = "target", AuthorizationMethods = "PBAC-Purpose", SortOrder = 70 },
                new() { Key = "radac", Title = "Risk Adaptive", Href = "/radac", Icon = "activity", AuthorizationMethods = "RAdAC", SortOrder = 80 },
                new() { Key = "rebac", Title = "Relations (ReBAC)", Href = "/rebac", Icon = "git-branch", AuthorizationMethods = "ReBAC", SortOrder = 90 },
                new() { Key = "pac", Title = "Privileged Access", Href = "/pac", Icon = "key", AuthorizationMethods = "PAC", SortOrder = 100 },
                new() { Key = "cbac", Title = "Context Policies", Href = "/cbac", Icon = "globe", AuthorizationMethods = "CBAC", SortOrder = 110 },
                new() { Key = "rubac", Title = "Access Rules", Href = "/rubac", Icon = "list", AuthorizationMethods = "RuBAC", SortOrder = 120 },
                new() { Key = "profile", Title = "My Access", Href = "/profile", Icon = "user", AuthorizationMethods = "CBAC", SortOrder = 200 },
            };

            context.MenuSections.AddRange(sections);
            await context.SaveChangesAsync();
        }

        // Grant Admin full section access (GOD already bypasses checks)
        if (admin is not null)
        {
            var allSections = await context.MenuSections.ToListAsync();
            foreach (var section in allSections)
            {
                var exists = await context.SectionPermissions
                    .AnyAsync(p => p.UserId == admin.Id && p.MenuSectionId == section.Id);
                if (!exists)
                {
                    context.SectionPermissions.Add(new SectionPermission
                    {
                        MenuSectionId = section.Id,
                        UserId = admin.Id,
                        CanRead = true,
                        CanWrite = true,
                        GrantedById = god?.Id ?? admin.Id
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
