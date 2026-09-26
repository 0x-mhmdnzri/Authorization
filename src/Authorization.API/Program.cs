using System.Text;
using Authorization.API.Data;
using Authorization.API.Models;
using Authorization.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Database (PostgreSQL) --------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// -------------------- Identity (Identity tables) --------------------
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// -------------------- JWT Authentication --------------------
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Authorization.API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Authorization.API";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// -------------------- Services --------------------
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAbacService, AbacService>();
builder.Services.AddScoped<IMacService, MacService>();
builder.Services.AddScoped<IDacService, DacService>();
builder.Services.AddScoped<IPbacService, PbacService>();
builder.Services.AddScoped<IPurposeService, PurposeService>();
builder.Services.AddScoped<IRadacService, RadacService>();
builder.Services.AddScoped<IRebacService, RebacService>();
builder.Services.AddScoped<IPacService, PacService>();
builder.Services.AddScoped<ICbacService, CbacService>();
builder.Services.AddScoped<IRubacService, RubacService>();

// -------------------- Controllers & OpenAPI --------------------
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// -------------------- Pipeline --------------------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // OpenAPI JSON available at /openapi/v1.json
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// -------------------- Seed data (RBAC + ABAC) --------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        // Ensure core roles exist
        string[] roles = { "Admin", "Manager", "User" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleName,
                    Description = $"{roleName} role for RBAC demo"
                });
            }
        }

        // Seed default Admin user
        const string adminEmail = "admin@authorization.local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Admin",
                Department = "IT",
                ClearanceLevel = "TopSecret",
                EmailConfirmed = true,
                IsActive = true
            };
            var createResult = await userManager.CreateAsync(adminUser, "Admin123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed a Finance user for ABAC demos
        const string financeEmail = "finance@authorization.local";
        var financeUser = await userManager.FindByEmailAsync(financeEmail);
        if (financeUser == null)
        {
            financeUser = new ApplicationUser
            {
                UserName = financeEmail,
                Email = financeEmail,
                FirstName = "Sara",
                LastName = "Finance",
                Department = "Finance",
                ClearanceLevel = "Confidential",
                EmailConfirmed = true,
                IsActive = true
            };
            var createResult = await userManager.CreateAsync(financeUser, "Finance123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(financeUser, "User");
            }
        }

        // Seed sample ABAC policies (only if none exist)
        if (!await context.AbacPolicies.AnyAsync())
        {
            context.AbacPolicies.AddRange(
                new AbacPolicy
                {
                    Name = "SameDepartment-Read",
                    Description = "Users can read resources that belong to their own department",
                    ResourceType = null,
                    Action = "Read",
                    Effect = "Allow",
                    Priority = 100,
                    RequireSameDepartment = true
                },
                new AbacPolicy
                {
                    Name = "Finance-Budget-BusinessHours",
                    Description = "Finance department can read Budget resources only during business hours",
                    ResourceType = "Budget",
                    Action = "Read",
                    Effect = "Allow",
                    Priority = 200,
                    RequireSameDepartment = true,
                    RequireBusinessHours = true,
                    AllowedDepartments = "Finance"
                },
                new AbacPolicy
                {
                    Name = "HighClearance-Confidential",
                    Description = "Users with Confidential+ clearance can read Confidential resources",
                    ResourceType = null,
                    Action = "Read",
                    Effect = "Allow",
                    Priority = 150,
                    MinimumClearance = "Confidential",
                    RequiredSensitivityMax = "Confidential"
                },
                new AbacPolicy
                {
                    Name = "Deny-Restricted-Without-TopSecret",
                    Description = "Explicit deny for Restricted resources unless TopSecret clearance",
                    ResourceType = null,
                    Action = "Read",
                    Effect = "Deny",
                    Priority = 300,
                    MinimumClearance = "TopSecret",
                    RequiredSensitivityMax = "Restricted"
                }
            );
            await context.SaveChangesAsync();
        }

        // Seed sample resources (only if none exist)
        if (!await context.Resources.AnyAsync())
        {
            var adminId = (await userManager.FindByEmailAsync(adminEmail))?.Id;
            context.Resources.AddRange(
                new Resource
                {
                    Name = "Q3 Budget Report",
                    ResourceType = "Budget",
                    OwnerDepartment = "Finance",
                    Sensitivity = "Confidential",
                    OwnerId = adminId
                },
                new Resource
                {
                    Name = "Public Company Handbook",
                    ResourceType = "Document",
                    OwnerDepartment = "HR",
                    Sensitivity = "Public",
                    OwnerId = adminId
                },
                new Resource
                {
                    Name = "Top Secret Project Plan",
                    ResourceType = "Document",
                    OwnerDepartment = "IT",
                    Sensitivity = "Restricted",
                    OwnerId = adminId
                }
            );
            await context.SaveChangesAsync();
        }

        // Seed sample PBAC policies (only if none exist)
        if (!await context.Policies.AnyAsync())
        {
            context.Policies.AddRange(
                new Policy
                {
                    Name = "Admin-FullAccess",
                    Description = "Admins can do anything",
                    ResourceType = "*",
                    Action = "*",
                    Effect = "Allow",
                    Priority = 1000,
                    RequiredRoles = "Admin"
                },
                new Policy
                {
                    Name = "Finance-Budget-Read",
                    Description = "Finance users can read Budget resources in business hours",
                    ResourceType = "Budget",
                    Action = "Read",
                    Effect = "Allow",
                    Priority = 200,
                    RequiredDepartments = "Finance",
                    RequireBusinessHours = true,
                    MaxResourceSensitivity = "Confidential"
                },
                new Policy
                {
                    Name = "SameDept-With-Dac",
                    Description = "Same department + explicit DAC grant required",
                    ResourceType = "*",
                    Action = "Read",
                    Effect = "Allow",
                    Priority = 150,
                    RequireSameDepartment = true,
                    RequireDacGrant = true
                },
                new Policy
                {
                    Name = "Deny-Restricted-Default",
                    Description = "Deny Restricted resources unless higher policy allows",
                    ResourceType = "*",
                    Action = "Read",
                    Effect = "Deny",
                    Priority = 50,
                    MaxResourceSensitivity = "Restricted"
                }
            );
            await context.SaveChangesAsync();
        }

        // Seed purposes (Purpose-Based AC)
        if (!await context.Purposes.AnyAsync())
        {
            var treatment = new Purpose { Code = "TREATMENT", Name = "Treatment", Description = "Clinical treatment of the patient" };
            var research = new Purpose { Code = "RESEARCH", Name = "Research", Description = "Scientific research (requires extra approval)" };
            var billing = new Purpose { Code = "BILLING", Name = "Billing", Description = "Insurance and billing operations" };
            var audit = new Purpose { Code = "AUDIT", Name = "Audit", Description = "Compliance and internal audit" };
            context.Purposes.AddRange(treatment, research, billing, audit);
            await context.SaveChangesAsync();

            // Link purposes to existing resources if any
            var resources = await context.Resources.ToListAsync();
            foreach (var res in resources)
            {
                // All resources allow TREATMENT and BILLING by default
                context.ResourcePurposes.Add(new ResourcePurpose { ResourceId = res.Id, PurposeId = treatment.Id });
                context.ResourcePurposes.Add(new ResourcePurpose { ResourceId = res.Id, PurposeId = billing.Id, AllowedRoles = "Admin,Manager" });
                // RESEARCH only on non-Restricted and requires consent
                if (res.Sensitivity != "Restricted" && res.Sensitivity != "TopSecret")
                {
                    context.ResourcePurposes.Add(new ResourcePurpose
                    {
                        ResourceId = res.Id,
                        PurposeId = research.Id,
                        AllowedRoles = "Admin",
                        RequiresExplicitConsent = true
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        // Seed default RAdAC policy
        if (!await context.RiskPolicies.AnyAsync())
        {
            context.RiskPolicies.Add(new RiskPolicy
            {
                Name = "Default-Risk-Policy",
                Description = "Standard thresholds: normal <=30, max acceptable 70, critical need >=80",
                NormalRiskThreshold = 30,
                MaxAcceptableRisk = 70,
                CriticalNeedThreshold = 80
            });
            await context.SaveChangesAsync();
        }

        // Seed ReBAC owner tuples for existing resources
        if (!await context.RelationTuples.AnyAsync())
        {
            var resources = await context.Resources.Where(r => r.OwnerId != null).ToListAsync();
            foreach (var res in resources)
            {
                context.RelationTuples.Add(new RelationTuple
                {
                    ObjectType = "document",
                    ObjectId = res.Id.ToString(),
                    Relation = "owner",
                    Subject = $"user:{res.OwnerId}",
                    CreatedBy = res.OwnerId
                });
            }
            await context.SaveChangesAsync();
        }

        // Seed PAC privilege definitions
        if (!await context.PrivilegeDefinitions.AnyAsync())
        {
            context.PrivilegeDefinitions.AddRange(
                new PrivilegeDefinition
                {
                    Code = "PROD_DB_ADMIN",
                    Name = "Production Database Admin",
                    Description = "Temporary admin access to production databases",
                    DefaultDurationMinutes = 60,
                    MaxDurationMinutes = 240,
                    AllowedRequesterRoles = "User,Manager,Admin",
                    ApproverRoles = "Admin,Manager",
                    RequiresApproval = true
                },
                new PrivilegeDefinition
                {
                    Code = "K8S_ADMIN",
                    Name = "Kubernetes Cluster Admin",
                    Description = "JIT admin on production Kubernetes",
                    DefaultDurationMinutes = 30,
                    MaxDurationMinutes = 120,
                    AllowedRequesterRoles = "Manager,Admin",
                    ApproverRoles = "Admin",
                    RequiresApproval = true
                },
                new PrivilegeDefinition
                {
                    Code = "SECRET_READ",
                    Name = "Read Secrets",
                    Description = "Temporary ability to read secret stores",
                    DefaultDurationMinutes = 15,
                    MaxDurationMinutes = 60,
                    AllowedRequesterRoles = "User,Manager,Admin",
                    ApproverRoles = "Admin",
                    RequiresApproval = true
                }
            );
            await context.SaveChangesAsync();
        }

        // Seed CBAC context policies
        if (!await context.ContextPolicies.AnyAsync())
        {
            context.ContextPolicies.AddRange(
                new ContextPolicy
                {
                    Name = "BusinessHours-TrustedNetwork",
                    Description = "Allow read during business hours from trusted network",
                    ResourceType = "*",
                    Action = "Read",
                    Effect = "Allow",
                    Priority = 100,
                    AllowedHourStart = 8,
                    AllowedHourEnd = 18,
                    AllowedDaysOfWeek = "Mon,Tue,Wed,Thu,Fri",
                    RequireTrustedNetwork = true
                },
                new ContextPolicy
                {
                    Name = "MFA-Required-For-Write",
                    Description = "Writes require MFA and managed device",
                    ResourceType = "*",
                    Action = "Write",
                    Effect = "Allow",
                    Priority = 200,
                    RequireManagedDevice = true,
                    MinimumAuthMethod = "MFA"
                },
                new ContextPolicy
                {
                    Name = "Block-Anomalous-Location",
                    Description = "Deny any access from anomalous locations",
                    ResourceType = "*",
                    Action = "*",
                    Effect = "Deny",
                    Priority = 500,
                    BlockAnomalousLocation = true
                },
                new ContextPolicy
                {
                    Name = "Weekend-Deny",
                    Description = "Deny access on weekends unless higher priority allows",
                    ResourceType = "*",
                    Action = "*",
                    Effect = "Deny",
                    Priority = 50,
                    AllowedDaysOfWeek = "Sat,Sun"
                }
            );
            await context.SaveChangesAsync();
        }

        // Seed RuBAC rules
        if (!await context.AccessRules.AnyAsync())
        {
            context.AccessRules.AddRange(
                new AccessRule
                {
                    Name = "Deny-Blocked-IPs",
                    Description = "Deny known bad IPs",
                    Priority = 1000,
                    Effect = "Deny",
                    SourceIpDenyList = "192.0.2.1,203.0.113.0/24"
                },
                new AccessRule
                {
                    Name = "BusinessHours-Allow",
                    Description = "Allow during business hours weekdays",
                    Priority = 100,
                    Effect = "Allow",
                    TimeStartHour = 8,
                    TimeEndHour = 18,
                    DaysOfWeek = "Mon,Tue,Wed,Thu,Fri"
                },
                new AccessRule
                {
                    Name = "Finance-Dept-Only-Budget",
                    Description = "Budget resources only for Finance department",
                    Priority = 200,
                    Effect = "Allow",
                    ResourceType = "Budget",
                    Action = "Read",
                    RequiredDepartment = "Finance"
                },
                new AccessRule
                {
                    Name = "Admin-Always",
                    Description = "Admins always allowed",
                    Priority = 900,
                    Effect = "Allow",
                    RequiredRole = "Admin"
                },
                new AccessRule
                {
                    Name = "RateLimit-General",
                    Description = "Max 100 requests per hour per user",
                    Priority = 50,
                    Effect = "Allow",
                    RateLimitPerHour = 100
                }
            );
            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();
