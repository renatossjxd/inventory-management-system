using InventoryManagement.Application.Abstractions;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InventoryDb")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:InventoryDb.");
        services.AddDbContext<InventoryDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IInventoryDbContext>(provider => provider.GetRequiredService<InventoryDbContext>());
        services.AddSingleton<IPasswordService, PasswordService>();
        return services;
    }
}
