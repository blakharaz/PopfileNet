using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PopfileNet.Database;

public static class DatabaseConfigurationExtensions
{
    public static void AddEmailAnalysisDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PopfileNetDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));
    }
}