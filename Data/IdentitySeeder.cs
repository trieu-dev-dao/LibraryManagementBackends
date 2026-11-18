
using LibraryManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LibraryManagement.Data;

public static class IdentitySeeder
{
    private static readonly string[] Roles =
    {
        "Admin",
        "Librarian",
        "User"
    };

    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();

        var services = scope.ServiceProvider;

        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager =
            services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager =
            services.GetRequiredService<UserManager<ApplicationUser>>();

        await ((DatabaseFacade)db.Database).MigrateAsync();

        // 1. Tạo các role nếu chưa tồn tại.
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole<int>(role));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Không thể tạo role {role}: " +
                        string.Join("; ",
                            result.Errors.Select(e => e.Description)));
                }
            }
        }

        // 2. Seed tài khoản Admin và Librarian.
        await EnsureAccountAsync(
            userManager,
            configuration,
            "Admin",
            "ADMIN_EMAIL",
            "ADMIN_PASSWORD",
            "ADMIN_FULLNAME",
            "Administrator");

        await EnsureAccountAsync(
            userManager,
            configuration,
            "Librarian",
            "LIBRARIAN_EMAIL",
            "LIBRARIAN_PASSWORD",
            "LIBRARIAN_FULLNAME",
            "Librarian");
    }

    private static async Task EnsureAccountAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        string role,
        string emailKey,
        string passwordKey,
        string fullNameKey,
        string defaultName)
    {
        // Đọc từ cấu hình trước, sau đó fallback sang environment variable.
        var email =
            configuration[$"SeedAccounts:{role}:Email"]
            ?? Environment.GetEnvironmentVariable(emailKey);

        var password =
            configuration[$"SeedAccounts:{role}:Password"]
            ?? Environment.GetEnvironmentVariable(passwordKey);

        var fullName =
            configuration[$"SeedAccounts:{role}:FullName"]
            ?? Environment.GetEnvironmentVariable(fullNameKey)
            ?? defaultName;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"Thiếu email hoặc password cho role {role}. " +
                $"Hãy cấu hình {emailKey} và {passwordKey}.");
        }

        var user = await userManager.FindByEmailAsync(email);

        // Chỉ tạo mới khi tài khoản chưa tồn tại.
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                Role = role
            };

            var createResult =
                await userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Không thể tạo tài khoản {role}: " +
                    string.Join("; ",
                        createResult.Errors.Select(e => e.Description)));
            }
        }
        else if (!string.Equals(user.Role, role,
                     StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Email {email} đang được dùng cho role " +
                $"{user.Role}, không thể seed thêm role {role}.");
        }

        // Bổ sung role nếu tài khoản chưa có role này.
        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult =
                await userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Không thể gán role {role}: " +
                    string.Join("; ",
                        roleResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
