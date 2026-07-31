using InventoryManagement.Application.Abstractions;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Infrastructure.Security;
using InventoryManagement.Infrastructure.Storage;
using Azure.Storage.Blobs;
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
        services.AddOptions<BlobStorageOptions>()
            .Bind(configuration.GetSection(BlobStorageOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Falta BlobStorage:ConnectionString.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ContainerName),
                "Falta BlobStorage:ContainerName.")
            .ValidateOnStart();
        services.AddSingleton(provider => new BlobServiceClient(
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<BlobStorageOptions>>()
                .Value.ConnectionString));
        services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
        return services;
    }
}
