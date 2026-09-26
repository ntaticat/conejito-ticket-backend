using ConejitoTicket.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConejitoTicket.Api.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = app.Configuration;

        await context.Database.MigrateAsync();

        var userName = config["Admin:UserName"];
        var password = config["Admin:Password"];
        if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password)
            && !await context.AdminUsers.AnyAsync(u => u.UserName == userName))
        {
            context.AdminUsers.Add(new AdminUser { UserName = userName, PasswordHash = Hasher.Hash(password) });
        }

        // Solo en desarrollo: app cliente de prueba para el flujo M2M.
        var clientId = config["SeedSystemApp:ClientId"];
        var clientSecret = config["SeedSystemApp:ClientSecret"];
        if (app.Environment.IsDevelopment()
            && !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret)
            && !await context.SystemApps.AnyAsync(s => s.ClientId == clientId))
        {
            context.SystemApps.Add(new SystemApp
            {
                Name = config["SeedSystemApp:Name"] ?? clientId,
                ClientId = clientId,
                ClientSecretHash = Hasher.Hash(clientSecret),
            });
        }

        await context.SaveChangesAsync();
    }
}
