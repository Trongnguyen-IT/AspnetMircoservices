using Microsoft.EntityFrameworkCore;

namespace Ordering.API.MigrationManager
{
    public static class MigrationManager
    {
        public static WebApplication MigrateDatabase<TContext>(this WebApplication app,
            Action<TContext, IServiceProvider> seeder) where TContext : DbContext
        {
            int retryForAvailability = 0;
            using (var scope = app.Services.CreateScope())
            {
                var services = app.Services;
                var logger = services.GetRequiredService<ILogger<TContext>>();
                var context = services.GetService<TContext>();

                try
                {
                    logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);

                    InvokeSeeder(seeder, context, services);

                    logger.LogInformation("Migrated database associated with context {DbContextName}", typeof(TContext).Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occured while migration the database used on context {DbContextName}", typeof(TContext).Name);
                    if (retryForAvailability < 50)
                    {
                        retryForAvailability++;
                        Thread.Sleep(2000);
                        MigrateDatabase(app, seeder);
                    };
                }
            }

            return app;
        }

        private static void InvokeSeeder<TContext>(Action<TContext, IServiceProvider> seeder,
            TContext context,
            IServiceProvider services) where TContext : DbContext
        {
            seeder(context, services);
        }
    }
}
