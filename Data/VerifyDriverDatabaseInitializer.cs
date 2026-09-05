using Microsoft.EntityFrameworkCore;
using VerifyDriversAPI.Models;

namespace VerifyDriversAPI.Data
{
    public static class VerifyDriverDatabaseInitializer
    {
        public static async Task InitializeVerifyDriverDatabaseAsync(
            this IServiceProvider services,
            IHostEnvironment environment)
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("VerifyDriverDatabaseInitializer");

            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pendingMigrations.Count > 0)
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Applied {MigrationCount} pending VerifyDriverAPI migration(s).", pendingMigrations.Count);
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            await CreateOperationalTablesAsync(context);

            if (context.Database.IsSqlite())
            {
                await context.Database.OpenConnectionAsync();
                await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
            }

            try
            {
                await SeedLegacyMarketplaceDataAsync(context);
                await SeedRelationshipDataAsync(context);
            }
            finally
            {
                if (context.Database.IsSqlite())
                {
                    await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
                }
            }
        }

        private static async Task CreateOperationalTablesAsync(AppDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS verification_cases (
                    case_id TEXT NOT NULL PRIMARY KEY,
                    case_type TEXT NOT NULL,
                    relationship_context TEXT NOT NULL,
                    primary_profile_id INTEGER NOT NULL,
                    counterparty TEXT NULL,
                    status TEXT NOT NULL,
                    privacy_status TEXT NOT NULL,
                    created_at_utc TEXT NOT NULL,
                    updated_at_utc TEXT NOT NULL
                );
                """);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS document_evidence (
                    document_id TEXT NOT NULL PRIMARY KEY,
                    case_id TEXT NOT NULL,
                    document_type TEXT NOT NULL,
                    file_name TEXT NOT NULL,
                    content_type TEXT NULL,
                    size_bytes INTEGER NULL,
                    publicly_visible INTEGER NOT NULL,
                    FOREIGN KEY(case_id) REFERENCES verification_cases(case_id)
                );
                """);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS counterparty_confirmations (
                    confirmation_id TEXT NOT NULL PRIMARY KEY,
                    case_id TEXT NOT NULL,
                    counterparty TEXT NOT NULL,
                    claim TEXT NOT NULL,
                    state TEXT NOT NULL,
                    FOREIGN KEY(case_id) REFERENCES verification_cases(case_id)
                );
                """);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS structured_feedback (
                    feedback_id TEXT NOT NULL PRIMARY KEY,
                    category TEXT NOT NULL,
                    severity INTEGER NOT NULL,
                    context TEXT NOT NULL,
                    related_profile_id INTEGER NULL,
                    related_entity TEXT NULL,
                    submitter_type TEXT NOT NULL,
                    moderation_status TEXT NOT NULL,
                    dispute_status TEXT NOT NULL,
                    submitted_at_utc TEXT NOT NULL
                );
                """);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS feedback_disputes (
                    dispute_id TEXT NOT NULL PRIMARY KEY,
                    feedback_id TEXT NOT NULL,
                    profile_id INTEGER NOT NULL,
                    reason TEXT NOT NULL,
                    requested_by TEXT NOT NULL,
                    status TEXT NOT NULL,
                    created_at_utc TEXT NOT NULL,
                    FOREIGN KEY(feedback_id) REFERENCES structured_feedback(feedback_id)
                );
                """);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS verification_case_disputes (
                    dispute_id TEXT NOT NULL PRIMARY KEY,
                    case_id TEXT NOT NULL,
                    profile_id INTEGER NOT NULL,
                    reason TEXT NOT NULL,
                    requested_by TEXT NOT NULL,
                    status TEXT NOT NULL,
                    created_at_utc TEXT NOT NULL,
                    FOREIGN KEY(case_id) REFERENCES verification_cases(case_id)
                );
                """);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS marketplace_relationships (
                    relationship_id TEXT NOT NULL PRIMARY KEY,
                    driver_user_id INTEGER NOT NULL,
                    owner_user_id INTEGER NULL,
                    vehicle_id INTEGER NULL,
                    platform_id INTEGER NULL,
                    partner_id INTEGER NULL,
                    relationship_type TEXT NOT NULL,
                    verification_status TEXT NOT NULL,
                    availability_status TEXT NOT NULL,
                    created_at_utc TEXT NOT NULL
                );
                """);
        }

        private static async Task SeedLegacyMarketplaceDataAsync(AppDbContext context)
        {
            await UpsertPlatformAsync(context, 1, "Bolt", "Rideshare");
            await UpsertPlatformAsync(context, 2, "Uber", "Rideshare");
            await UpsertPlatformAsync(context, 3, "FleetLink", "Fleet");
            await UpsertPlatformAsync(context, 4, "RoadFreight", "Trucking");

            await UpsertUserTypeAsync(context, 1, "Rideshare");
            await UpsertUserTypeAsync(context, 2, "Delivery");
            await UpsertUserTypeAsync(context, 3, "Trucking");
            await UpsertUserTypeAsync(context, 4, "Owner");
            await UpsertUserTypeAsync(context, 5, "Fleet");

            await UpsertVehicleAsync(context, 1, "GP42MX", "Toyota", "Corolla Quest", "2020", 1, 10);
            await UpsertVehicleAsync(context, 2, "LJ18GP", "Hyundai", "H100", "2021", 3, 11);
            await UpsertVehicleAsync(context, 3, "ND18LK", "Freightliner", "Argosy", "2018", 4, 12);
            await UpsertVehicleAsync(context, 4, "CA77DD", "Isuzu", "D-Max", "2022", 2, 13);

            await UpsertUserAsync(context, 10, "Thabo Mokoena", "Male", 34, 4.2m, 1, 10, 1, 1);
            await UpsertUserAsync(context, 11, "Nomsa Dlamini", "Female", 29, 4.6m, 2, 11, 2, 3);
            await UpsertUserAsync(context, 12, "Sipho Khumalo", "Male", 42, 3.6m, 3, 12, 3, 4);
            await UpsertUserAsync(context, 13, "Aisha Patel", "Female", 38, 4.8m, 4, 13, 4, 2);

            await UpsertCommentAsync(context, 1, "Safe driver", 10, 10);
            await UpsertCommentAsync(context, 2, "Fleet verified", 11, 11);
            await UpsertCommentAsync(context, 3, "Review docs", 12, 12);
        }

        private static async Task SeedRelationshipDataAsync(AppDbContext context)
        {
            var now = DateTimeOffset.UtcNow.ToString("O");

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO marketplace_relationships
                    (relationship_id, driver_user_id, owner_user_id, vehicle_id, platform_id, partner_id, relationship_type, verification_status, availability_status, created_at_utc)
                SELECT {"rel-rideshare-10"}, {10}, {10}, {1}, {1}, {10}, {"Employment"}, {"Verified"}, {"Available"}, {now}
                WHERE NOT EXISTS (SELECT 1 FROM marketplace_relationships WHERE relationship_id = {"rel-rideshare-10"});
                """);

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO marketplace_relationships
                    (relationship_id, driver_user_id, owner_user_id, vehicle_id, platform_id, partner_id, relationship_type, verification_status, availability_status, created_at_utc)
                SELECT {"rel-fleet-11"}, {11}, {13}, {2}, {3}, {13}, {"Fleet contract"}, {"CounterpartyReview"}, {"Available"}, {now}
                WHERE NOT EXISTS (SELECT 1 FROM marketplace_relationships WHERE relationship_id = {"rel-fleet-11"});
                """);

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO marketplace_relationships
                    (relationship_id, driver_user_id, owner_user_id, vehicle_id, platform_id, partner_id, relationship_type, verification_status, availability_status, created_at_utc)
                SELECT {"rel-trucking-12"}, {12}, {13}, {3}, {4}, {13}, {"Owner/fleet agreement"}, {"Disputed"}, {"ReviewRequired"}, {now}
                WHERE NOT EXISTS (SELECT 1 FROM marketplace_relationships WHERE relationship_id = {"rel-trucking-12"});
                """);
        }

        private static async Task UpsertPlatformAsync(AppDbContext context, int id, string name, string type)
        {
            if (!await context.Platforms.AnyAsync(item => item.pID == id))
            {
                context.Platforms.Add(new Platform { pID = id, pName = name, pType = type });
                await context.SaveChangesAsync();
                return;
            }

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE _Platform SET pName = {name}, pType = {type} WHERE pID = {id};");
        }

        private static async Task UpsertUserTypeAsync(AppDbContext context, int id, string description)
        {
            if (!await context.UserTypes.AnyAsync(item => item.U_T_ID == id))
            {
                context.UserTypes.Add(new UserType { U_T_ID = id, U_T_description = description });
                await context.SaveChangesAsync();
                return;
            }

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE _User_types SET U_T_description = {description} WHERE U_T_ID = {id};");
        }

        private static async Task UpsertVehicleAsync(
            AppDbContext context,
            int id,
            string registration,
            string make,
            string model,
            string year,
            int platformId,
            int partnerId)
        {
            if (!await context.Vehicles.AnyAsync(item => item.vID == id))
            {
                context.Vehicles.Add(new Vehicle
                {
                    vID = id,
                    vregistration = registration,
                    vMake = make,
                    vModel_name = model,
                    vModel_year = year,
                    vPlatform_ID = platformId,
                    vPartner_ID = partnerId
                });
                await context.SaveChangesAsync();
                return;
            }

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE _Vehicle
                SET vregistration = {registration},
                    vMake = {make},
                    vModel_name = {model},
                    vModel_year = {year},
                    vPlatform_ID = {platformId},
                    vParter_ID = {partnerId}
                WHERE vID = {id};
                """);
        }

        private static async Task UpsertUserAsync(
            AppDbContext context,
            int id,
            string name,
            string gender,
            int age,
            decimal rating,
            int vehicleId,
            int partnerId,
            int userTypeId,
            int platformId)
        {
            if (!await context.Users.AnyAsync(item => item.uID == id))
            {
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO _User (uID, uNames, uGender, uAge, uRating, uVID, uPartner_ID, uUsertype_ID, uPlatform_ID)
                    VALUES ({id}, {name}, {gender}, {age}, {rating}, {vehicleId}, {partnerId}, {userTypeId}, {platformId});
                    """);
                return;
            }

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE _User
                SET uNames = {name},
                    uGender = {gender},
                    uAge = {age},
                    uRating = {rating},
                    uVID = {vehicleId},
                    uPartner_ID = {partnerId},
                    uUsertype_ID = {userTypeId},
                    uPlatform_ID = {platformId}
                WHERE uID = {id};
                """);
        }

        private static async Task UpsertCommentAsync(AppDbContext context, int id, string text, int userId, int partnerId)
        {
            if (!await context.Comments.AnyAsync(item => item.cID == id))
            {
                context.Comments.Add(new Comment
                {
                    cID = id,
                    cText = text,
                    cDateTime = DateTime.UtcNow,
                    c_Uid = userId,
                    c_Pid = partnerId
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
