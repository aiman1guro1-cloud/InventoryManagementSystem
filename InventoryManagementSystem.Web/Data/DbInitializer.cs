using System;
using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;
using InventoryManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Web.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Apply migrations automatically
            await context.Database.MigrateAsync();

            // Ensure roles exist
            string[] roles = { "Admin", "Manager", "Staff" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create default users if none exist
            if (!await userManager.Users.AnyAsync())
            {
                // FIXED DATE (Prevents migration seed warnings)
                var fixedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                // =========================
                // ADMIN USER
                // =========================
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@inventory.com",
                    Email = "admin@inventory.com",
                    FirstName = "System",
                    LastName = "Administrator",
                    CreatedAt = fixedDate,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                // =========================
                // MANAGER USER
                // =========================
                var managerUser = new ApplicationUser
                {
                    UserName = "manager@inventory.com",
                    Email = "manager@inventory.com",
                    FirstName = "Store",
                    LastName = "Manager",
                    CreatedAt = fixedDate,
                    IsActive = true,
                    EmailConfirmed = true
                };

                result = await userManager.CreateAsync(managerUser, "Manager@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "Manager");
                }

                // =========================
                // STAFF USER
                // =========================
                var staffUser = new ApplicationUser
                {
                    UserName = "staff@inventory.com",
                    Email = "staff@inventory.com",
                    FirstName = "Regular",
                    LastName = "Staff",
                    CreatedAt = fixedDate,
                    IsActive = true,
                    EmailConfirmed = true
                };

                result = await userManager.CreateAsync(staffUser, "Staff@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(staffUser, "Staff");
                }
            }
        }
    }
}