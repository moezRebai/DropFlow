using DropFlow.Domain.Constants;
using DropFlow.Domain.Entities;
using DropFlow.Shared.Enums;
using DropFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Bogus;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DropFlow.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();

            // 1. Seed Roles
            await SeedRolesAsync(services, logger);

            // 2. SEED SUPER ADMIN
            await SeedSuperAdminAsync(services, logger);

            // 3. Seed Dev Data
            if (app.Environment.IsDevelopment())
            {
                await SeedDevelopmentDataAsync(services, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing database");
            throw;
        }
    }

    private static async Task SeedSuperAdminAsync(
        IServiceProvider services,
        ILogger logger)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        logger.LogInformation("Checking for DropFlow Super Admin...");

        var adminEmail = configuration["SuperAdmin:Email"] ?? "admin@dropflow.com";
        var adminPassword = configuration["SuperAdmin:Password"] ?? "Admin@DropFlow123";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

        if (existingAdmin != null)
        {
            logger.LogInformation("Super Admin already exists");
            return;
        }

        logger.LogInformation("Creating DropFlow Super Admin...");

        // Créer avec TenantId = 0
        var admin = ApplicationUser.Create(
            tenantId: TenantIds.DropFlowAdmin, // 0
            email: adminEmail,
            firstName: "DropFlow",
            lastName: "Admin"
        );

        var result = await userManager.CreateAsync(admin, adminPassword);

        if (!result.Succeeded)
        {
            logger.LogError("Failed to create Super Admin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        // Assigner rôle Admin
        await userManager.AddToRoleAsync(admin, Roles.Admin);

        logger.LogInformation("Super Admin created successfully: {Email}", adminEmail);
        logger.LogWarning("CHANGE DEFAULT PASSWORD IN PRODUCTION!");
    }

    private static async Task SeedRolesAsync(IServiceProvider services, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        logger.LogInformation("Seeding roles...");

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                {
                    logger.LogInformation("Role created: {Role}", role);
                }
                else
                {
                    logger.LogError("Failed to create role {Role}: {Errors}",
                        role, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        logger.LogInformation("Roles seeding completed");
    }

    private static async Task CleanDevelopmentDataAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Cleaning all development data...");

        // Delete in FK-safe order (children before parents)
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""DeliveryItems""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""RouteTeam""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""TimeSlots""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Deliveries""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Routes""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""ClientAddresses""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Clients""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Drivers""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Vehicles""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Stores""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""TenantDepots""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""AuditLogs""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""UserInvitations""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""UserPreferences""");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""RefreshTokens""");
        await context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM ""AspNetUserRoles""
            WHERE ""UserId"" IN (SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""TenantId"" != 0)");
        await context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM ""AspNetUserClaims""
            WHERE ""UserId"" IN (SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""TenantId"" != 0)");
        await context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM ""AspNetUserLogins""
            WHERE ""UserId"" IN (SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""TenantId"" != 0)");
        await context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM ""AspNetUserTokens""
            WHERE ""UserId"" IN (SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""TenantId"" != 0)");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""AspNetUsers"" WHERE ""TenantId"" != 0");
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Tenants""");

        logger.LogInformation("Development data cleaned successfully");
    }

    private static async Task SeedDevelopmentDataAsync(IServiceProvider services, ILogger logger)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        var forceReseed = configuration.GetValue("DevData:ForceReseed", false);

        if (await context.Tenants.AnyAsync())
        {
            if (!forceReseed)
            {
                logger.LogInformation("Development data already exists, skipping seed");
                return;
            }

            logger.LogInformation("DevData:ForceReseed=true — cleaning existing data before re-seeding...");
            await CleanDevelopmentDataAsync(context, logger);
        }

        logger.LogInformation("Seeding development data...");

        // 1. Créer un tenant de test
        var testTenant = Tenant.Create("Test Company");
        context.Tenants.Add(testTenant);
        await context.SaveChangesAsync();

        testTenant.UpdateCompanyInfo(
            companyName: "Test Company SARL",
            address: "123 Avenue des Tests",
            zipCode: "75001",
            city: "Paris",
            phone: "01 23 45 67 89",
            email: "contact@testcompany.fr",
            website: "https://testcompany.fr"
        );

        testTenant.UpdateLegalInfo(
            siret: "12345678901234",
            vatNumber: "FR12345678901",
            legalForm: "SARL",
            legalMentions: "SARL au capital de 10 000€ - RCS Paris",
            bankDetails: "FR76 1234 5678 9012 3456 7890 123"
        );

        await context.SaveChangesAsync();

        // Seed des dépôts
        await SeedDepotsAsync(context, testTenant.Id, "system", logger);

        // 2. Créer un utilisateur Manager de test
        var testUser = ApplicationUser.Create(
            tenantId: testTenant.Id,
            email: "defaultManager@test.com",
            firstName: "John",
            lastName: "Dupont"
        );

        await userManager.CreateAsync(testUser, "Test@123");
        await userManager.AddToRoleAsync(testUser, Roles.Manager);

        await SeedVehiclesAndDriversAsync(context, testTenant.Id, testUser.Id, logger);

        // 3. SEED STORES
        var stores = new[]
        {
            new Store
            {
                TenantId = testTenant.Id,
                Name = "Conforama Paris 15",
                Address = "123 Rue de la Convention",
                ZipCode = "75015",
                City = "Paris",
                ContactName = "Marie Dupont",
                Phone = "0145678901",
                Email = "paris15@conforama.fr",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = testUser.Id
            },
            new Store
            {
                TenantId = testTenant.Id,
                Name = "BUT Nanterre",
                Address = "456 Avenue Georges Clemenceau",
                ZipCode = "92000",
                City = "Nanterre",
                ContactName = "Jean Martin",
                Phone = "0147890123",
                Email = "nanterre@but.fr",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = testUser.Id
            },
            new Store
            {
                TenantId = testTenant.Id,
                Name = "IKEA Vélizy",
                Address = "789 Avenue de l'Europe",
                ZipCode = "78140",
                City = "Vélizy-Villacoublay",
                ContactName = "Sophie Leroux",
                Phone = "0139456789",
                Email = "velizy@ikea.fr",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = testUser.Id
            }
        };

        context.Stores.AddRange(stores);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded 3 test stores");

        // 4. SEED CLIENTS avec Bogus
        await SeedClientsAsync(context, testTenant.Id, testUser.Id, logger);

        // 5. SEED DELIVERIES avec Bogus
        await SeedDeliveriesAsync(context, testTenant.Id, testUser.Id, stores, logger);

        // 6. SEED AUDIT LOGS pour les notifications du dashboard
        await SeedAuditLogsAsync(context, testTenant.Id, testUser.Id, logger);

        logger.LogInformation("Development data seeded successfully");
    }

    // ----------------------------------------------------------------
    // SEED CLIENTS avec Bogus
    // ----------------------------------------------------------------

    private static async Task SeedClientsAsync(
        ApplicationDbContext context,
        int tenantId,
        string userId,
        ILogger logger)
    {
        logger.LogInformation("Seeding clients with Bogus...");

        Randomizer.Seed = new Random(123);

        var clientFaker = new Faker<Client>("fr")
            .RuleFor(c => c.TenantId, _ => tenantId)
            .RuleFor(c => c.FirstName, f => f.Name.FirstName())
            .RuleFor(c => c.LastName, f => f.Name.LastName())
            .RuleFor(c => c.Email, (f, c) =>
                f.Internet.Email(c.FirstName, c.LastName).ToLower())
            .RuleFor(c => c.Phone, f => f.Phone.PhoneNumber("01########"))
            .RuleFor(c => c.IsActive, _ => true)
            .RuleFor(c => c.CreatedDate, _ => DateTime.UtcNow)
            .RuleFor(c => c.ModifiedDate, _ => DateTime.UtcNow)
            .RuleFor(c => c.CreatedBy, _ => userId)
            .RuleFor(c => c.ModifiedBy, _ => userId);

        var clients = clientFaker.Generate(20);
        context.Clients.AddRange(clients);
        await context.SaveChangesAsync();

        var addressFaker = new Faker<ClientAddress>("fr")
            .RuleFor(ca => ca.Label, f => f.PickRandom(
                "Domicile", "Bureau", "Entrepôt", "Résidence secondaire"))
            .RuleFor(ca => ca.Address, f => f.Address.StreetAddress())
            .RuleFor(ca => ca.ZipCode, f => f.Address.ZipCode("7####"))
            .RuleFor(ca => ca.City, f => f.PickRandom(
                "Paris", "Reims", "Épernay", "Châlons-en-Champagne", "Troyes",
                "Vitry-le-François", "Saint-Dizier", "Romilly-sur-Seine",
                "Sainte-Savine", "La Chapelle-Saint-Luc"
            ))
            .RuleFor(ca => ca.Latitude, f => f.Address.Latitude())
            .RuleFor(ca => ca.Longitude, f => f.Address.Longitude())
            .RuleFor(ca => ca.Complement, f => f.Random.Bool(0.3f) ? f.Address.SecondaryAddress() : null)
            .RuleFor(ca => ca.IsDefault, (f, ca) => true);

        foreach (var client in clients)
        {
            var addresses = addressFaker
                .RuleFor(ca => ca.ClientId, _ => client.Id)
                .Generate(new Faker().Random.Int(1, 2));

            if (addresses.Count > 1)
            {
                addresses[1].IsDefault = false;
            }

            context.ClientAddresses.AddRange(addresses);
        }

        await context.SaveChangesAsync();

        logger.LogInformation("Seeded 20 clients with addresses");
    }

    // ----------------------------------------------------------------
    // SEED DELIVERIES avec Bogus
    // ----------------------------------------------------------------

    private static async Task SeedDeliveriesAsync(
        ApplicationDbContext context,
        int tenantId,
        string userId,
        Store[] stores,
        ILogger logger)
    {
        logger.LogInformation("Seeding deliveries with Bogus...");

        var allClients = await context.Clients
            .Include(c => c.Addresses)
            .Where(c => c.TenantId == tenantId)
            .ToListAsync();

        if (allClients.Count < 10)
        {
            logger.LogWarning("Not enough clients to seed 10 deliveries, skipping");
            return;
        }

        Randomizer.Seed = new Random(456);
        var faker = new Faker("fr");

        var selectedClients = allClients.OrderBy(_ => faker.Random.Int()).Take(10).ToList();

        var configs = new (DeliveryStatus Status, int? DaysOffset)[]
        {
            (DeliveryStatus.ToBePlanned, null),
            (DeliveryStatus.ToBePlanned, null),
            (DeliveryStatus.ToBePlanned, null),
            (DeliveryStatus.ToBePlanned, 3),
            (DeliveryStatus.ToBePlanned, 7),
            (DeliveryStatus.ToBePlanned, 14),
            (DeliveryStatus.ToBePlanned, 21),
            (DeliveryStatus.ToBePlanned, 30),
            (DeliveryStatus.InProgress,  1),
            (DeliveryStatus.InProgress,  2),
        };

        var sequentialNumber = 5000;
        var deliveries = new List<Delivery>();

        for (int i = 0; i < 10; i++)
        {
            var client = selectedClients[i];
            var address = client.Addresses.First(a => a.IsDefault);
            var store = faker.PickRandom(stores);
            var (status, daysOffset) = configs[i];

            DateTime? scheduledDate = daysOffset.HasValue
                ? DateTime.UtcNow.Date.AddDays(daysOffset.Value)
                : null;

            var clientAmount = Math.Round(faker.Random.Decimal(100, 600), 0);
            var storeAmount  = Math.Round(faker.Random.Decimal(100, 600), 0);

            deliveries.Add(new Delivery
            {
                TenantId              = tenantId,
                SequentialNumber      = sequentialNumber,
                Reference             = $"DL-{DateTime.UtcNow.Year}-{sequentialNumber:D4}",
                Status                = status,
                ClientId              = client.Id,
                ClientAddressId       = address.Id,
                StoreId               = store.Id,
                FileNumber            = $"D-{faker.Random.Int(1000, 9999)}",
                ScheduledDate         = scheduledDate,
                EstimatedDurationMinutes = faker.Random.Int(30, 120),
                ClientPaymentAmount   = clientAmount,
                StorePaymentAmount    = storeAmount,
                Price                 = clientAmount + storeAmount,
                WithAssembly          = faker.Random.Bool(0.3f),
                DeliveryNotes = faker.Random.Bool(0.4f)
                    ? faker.PickRandom(
                        "Sonner 2 fois",
                        "Livrer à l'arrière du bâtiment",
                        "Code portail: 1234",
                        "Appeler avant",
                        "Déposer devant la porte")
                    : null,
                InternalNotes = faker.Random.Bool(0.2f)
                    ? faker.PickRandom(
                        "Client difficile",
                        "Fragile",
                        "Prioritaire",
                        "Attention chien")
                    : null,
                CreatedDate  = DateTime.UtcNow.AddDays(faker.Random.Int(-30, 0)),
                CreatedBy    = userId,
                ModifiedDate = DateTime.UtcNow,
                ModifiedBy   = userId,
            });

            sequentialNumber++;
        }

        context.Deliveries.AddRange(deliveries);
        await context.SaveChangesAsync();

        await SeedDeliveryItemsAsync(context, deliveries, logger);

        logger.LogInformation("Seeded {Count} deliveries", deliveries.Count);
    }

    private static async Task SeedDeliveryItemsAsync(
        ApplicationDbContext context,
        List<Delivery> deliveries,
        ILogger logger)
    {
        var faker = new Faker("fr");
        var items = new List<DeliveryItem>();

        var furnitureItems = new[]
        {
            "Canapé 3 places",
            "Table basse",
            "Bibliothèque",
            "Lit 160x200",
            "Matelas",
            "Armoire 3 portes",
            "Commode",
            "Bureau",
            "Chaise de bureau",
            "Table à manger",
            "Chaise",
            "Meuble TV",
            "Étagère murale",
            "Buffet",
            "Fauteuil"
        };

        foreach (var delivery in deliveries)
        {
            var itemCount = faker.Random.Int(1, 4);

            for (int i = 0; i < itemCount; i++)
            {
                var quantity = faker.Random.Int(1, 3);

                var item = new DeliveryItem
                {
                    DeliveryId = delivery.Id,
                    Reference = faker.Random.Bool(0.6f) ? $"REF-{faker.Random.Int(1000, 9999)}" : null,
                    Designation = faker.PickRandom(furnitureItems),
                    Quantity = quantity,
                    Information = faker.Random.Bool(0.3f)
                        ? faker.PickRandom(
                            "Coloris: Gris anthracite",
                            "Matière: Tissu",
                            "Dimensions: 200x90x80",
                            "Avec garantie 2 ans"
                        )
                        : null
                };

                items.Add(item);
            }
        }

        context.DeliveryItems.AddRange(items);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} delivery items", items.Count);
    }

    // ----------------------------------------------------------------
    // SEED VEHICLES, DRIVERS
    // ----------------------------------------------------------------

    private static async Task SeedVehiclesAndDriversAsync(
        ApplicationDbContext context,
        int tenantId,
        string userId,
        ILogger logger)
    {
        logger.LogInformation("Seeding vehicles and drivers...");

        var faker = new Faker("fr");

        var vehicles = new[]
        {
            Vehicle.Create(
                brand: "Renault",
                model: "Master L3H2",
                plateNumber: "AB-123-CD",
                maxDeliveries: 15,
                maxVolume: 12
            ),
            Vehicle.Create(
                brand: "Peugeot",
                model: "Boxer L2H2",
                plateNumber: "EF-456-GH",
                maxDeliveries: 12,
                maxVolume: 10
            ),
            Vehicle.Create(
                brand: "Citroën",
                model: "Jumper L4H3",
                plateNumber: "IJ-789-KL",
                maxDeliveries: 18,
                maxVolume: 15
            )
        };

        foreach (var vehicle in vehicles)
        {
            typeof(Vehicle).GetProperty("TenantId")!
                .SetValue(vehicle, tenantId);
        }

        context.Vehicles.AddRange(vehicles);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} vehicles", vehicles.Length);

        var userManager = context.GetService<UserManager<ApplicationUser>>();

        var driverUsers = new[]
        {
            ("Jean", "Martin", "jean.martin@test.com"),
            ("Marc", "Dubois", "marc.dubois@test.com"),
            ("Luc", "Bernard", "luc.bernard@test.com"),
            ("Paul", "Petit", "paul.petit@test.com")
        };

        var createdDriverUsers = new List<ApplicationUser>();

        foreach (var (firstName, lastName, email) in driverUsers)
        {
            var user = ApplicationUser.Create(
                tenantId: tenantId,
                email: email,
                firstName: firstName,
                lastName: lastName
            );

            await userManager.CreateAsync(user, "Driver@123");
            await userManager.AddToRoleAsync(user, Roles.Livreur);

            createdDriverUsers.Add(user);
        }

        var drivers = new List<Driver>();

        for (int i = 0; i < createdDriverUsers.Count; i++)
        {
            var driver = Driver.Create(
                userId: createdDriverUsers[i].Id,
                licenseNumber: $"LIC{faker.Random.Int(100000, 999999)}",
                licenseExpiryDate: DateTime.UtcNow.Date.AddYears(faker.Random.Int(1, 5))
            );

            typeof(Driver).GetProperty("TenantId")!
                .SetValue(driver, tenantId);

            drivers.Add(driver);
        }

        context.Drivers.AddRange(drivers);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} drivers", drivers.Count);
    }

    /// <summary>
    /// Seed les dépôts de test pour le tenant
    /// </summary>
    private static async Task SeedDepotsAsync(
        ApplicationDbContext context,
        int tenantId,
        string userId,
        ILogger logger)
    {
        logger.LogInformation("Seeding tenant depots...");

        if (await context.TenantDepots.AnyAsync(d => d.TenantId == tenantId))
        {
            logger.LogInformation("Depots already exist for tenant, skipping");
            return;
        }

        var depots = new[]
        {
            TenantDepot.Create(
                tenantId: tenantId,
                name: "Dépôt Principal Paris 15",
                fullAddress: "123 Rue de l'Entrepôt, 75015 Paris",
                city: "Paris",
                zipCode: "75015",
                latitude: 48.8566,
                longitude: 2.3522,
                isDefault: true
            ),

            TenantDepot.Create(
                tenantId: tenantId,
                name: "Dépôt Vélizy",
                fullAddress: "789 Avenue de l'Europe, 78140 Vélizy-Villacoublay",
                city: "Vélizy-Villacoublay",
                zipCode: "78140",
                latitude: 48.7834,
                longitude: 2.1848,
                isDefault: false
            ),

            TenantDepot.Create(
                tenantId: tenantId,
                name: "Dépôt Nanterre",
                fullAddress: "456 Avenue Georges Clemenceau, 92000 Nanterre",
                city: "Nanterre",
                zipCode: "92000",
                latitude: 48.8923,
                longitude: 2.2078,
                isDefault: false
            )
        };

        context.TenantDepots.AddRange(depots);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} tenant depots", depots.Length);
    }

    private static async Task SeedAuditLogsAsync(
        ApplicationDbContext context,
        int tenantId,
        string userId,
        ILogger logger)
    {
        logger.LogInformation("Seeding audit logs...");

        var now = DateTime.UtcNow;

        var logs = new[]
        {
            AuditLog.Create(tenantId, userId, "Created",       "Delivery", changes: "Nouvelle livraison créée pour Jean Martin"),
            AuditLog.Create(tenantId, userId, "Completed",     "Route",    changes: "Tournée du jour démarrée avec succès"),
            AuditLog.Create(tenantId, userId, "StatusUpdated", "Delivery", changes: "Livraison marquée comme Livrée"),
            AuditLog.Create(tenantId, userId, "Warning",       "Delivery", changes: "3 livraisons non planifiées depuis plus de 14 jours"),
            AuditLog.Create(tenantId, userId, "Created",       "Route",    changes: "Nouvelle tournée planifiée pour demain"),
            AuditLog.Create(tenantId, userId, "StatusUpdated", "Delivery", changes: "Livraison en cours de livraison"),
            AuditLog.Create(tenantId, userId, "Created",       "Client",   changes: "Nouveau client ajouté : Dupont"),
        };

        var timestampProp = typeof(AuditLog).GetProperty("Timestamp")!;
        for (int i = 0; i < logs.Length; i++)
        {
            timestampProp.SetValue(logs[i], now.AddMinutes(-(i * 25)));
        }

        context.AuditLogs.AddRange(logs);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} audit logs", logs.Length);
    }
}
