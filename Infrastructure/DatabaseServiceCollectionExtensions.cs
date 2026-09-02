using Microsoft.EntityFrameworkCore;
using VerifyDriversAPI.Data;
using VerifyDriversAPI.Services;

namespace VerifyDriversAPI.Infrastructure
{
    public static class DatabaseServiceCollectionExtensions
    {
        public static IServiceCollection AddVerifyDriverDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                var provider = configuration["DatabaseProvider"] ?? "Sqlite";

                if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    var connectionString = configuration.GetConnectionString("SqliteConnection")
                        ?? "Data Source=Data/VerifyDriver.db";
                    options.UseSqlite(connectionString);
                    return;
                }

                var defaultConnection = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("DefaultConnection is required when DatabaseProvider is not Sqlite.");

                options.UseMySql(defaultConnection, new MySqlServerVersion(new Version(8, 0, 25)));
            });

            services.AddScoped<ITrustProfileService, TrustProfileService>();
            services.AddSingleton<IFeedbackModerationService, FeedbackModerationService>();
            services.AddSingleton<IVerificationCaseService, VerificationCaseService>();

            return services;
        }
    }
}
