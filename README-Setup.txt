LibraryManagement JWT + Controllers setup

1. Copy files into existing project. Add to ApplicationDbContext: public DbSet<AppUser> Users { get; set; } = null!;
2. Install: dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
3. appsettings.json: "JwtSettings": { "SecretKey": "REPLACE_WITH_PRIVATE_RANDOM_SECRET_AT_LEAST_32_BYTES", "Issuer": "LibraryManagement", "Audience": "LibraryManagementUsers", "ExpirationInMinutes": 60 }
4. Program.cs usings: using System.Text; using LibraryManagement.Models; using LibraryManagement.Security; using LibraryManagement.Services; using Microsoft.AspNetCore.Authentication.JwtBearer; using Microsoft.AspNetCore.Identity; using Microsoft.IdentityModel.Tokens;
5. Before builder.Build():
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
var jwt=builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>o.TokenValidationParameters=new TokenValidationParameters { ValidateIssuer=true,ValidIssuer=jwt.Issuer,ValidateAudience=true,ValidAudience=jwt.Audience,ValidateIssuerSigningKey=true,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),ValidateLifetime=true,ClockSkew=TimeSpan.FromSeconds(30),RoleClaimType=System.Security.Claims.ClaimTypes.Role,NameClaimType=System.Security.Claims.ClaimTypes.Name });
builder.Services.AddAuthorization();
6. Pipeline: app.UseAuthentication(); then app.UseAuthorization(); then app.MapControllers();
7. Ensure all five services and repositories are registered as in earlier steps.
8. Migration: dotnet ef migrations add AddAppUsers; dotnet ef database update
9. POST /api/auth/register {"username":"reader1","password":"StrongPassword123!"}; POST /api/auth/login with same JSON. Use returned token in Swagger Authorize as: Bearer <token>.
10. Controllers are protected by [Authorize]. Public registration always sets role User; do not let clients set Admin. Admin account should be provisioned securely.
