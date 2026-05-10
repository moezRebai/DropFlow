using DropFlow.Domain.Constants;
using DropFlow.Domain.Entities;
using DropFlow.Domain.Enums;
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

            if (app.Environment.IsDevelopment())
            {
                logger.LogInformation("Applying database migrations...");
                await context.Database.MigrateAsync();
            }

            // 1. Seed Roles
            await SeedRolesAsync(services, logger);

            // 2. ✅ SEED SUPER ADMIN
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

    // ✅ NOUVEAU
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

        // ✅ Créer avec TenantId = 0
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

        logger.LogInformation("✅ Super Admin created successfully: {Email}", adminEmail);
        logger.LogWarning("⚠️  CHANGE DEFAULT PASSWORD IN PRODUCTION!");
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

    private static async Task SeedDevelopmentDataAsync(IServiceProvider services, ILogger logger)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Vérifier si des données existent déjà
        if (await context.Tenants.AnyAsync())
        {
            logger.LogInformation("Development data already exists, skipping seed");
            return;
        }

        logger.LogInformation("Seeding development data...");

        // 1. Créer un tenant de test
        var testTenant = Tenant.Create("Test Company");
        context.Tenants.Add(testTenant);
        await context.SaveChangesAsync();

        // ✨ AJOUTER ICI - Initialiser les infos entreprise
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

        // ✨ AJOUTER ICI - Seed des dépôts
        await SeedDepotsAsync(context, testTenant.Id, "system", logger);
        
        // 2. Créer un utilisateur Manager de test
        var testUser = ApplicationUser.Create(
            tenantId: testTenant.Id,
            email: "manager@test.com",
            firstName: "Test",
            lastName: "Manager"
        );

        await userManager.CreateAsync(testUser, "Test@123");
        await userManager.AddToRoleAsync(testUser, Roles.Manager);

        await SeedVehiclesAndDriversAsync(context, testTenant.Id, testUser.Id, logger);
        
        // 3. ✅ SEED STORES
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

        logger.LogInformation("✅ Seeded 3 test stores");

        // 4. ✅ SEED CLIENTS avec Bogus
        await SeedClientsAsync(context, testTenant.Id, testUser.Id, logger);

        // 5. ✅ SEED DELIVERIES avec Bogus
        await SeedDeliveriesAsync(context, testTenant.Id, testUser.Id, stores, logger);

        // 6. ✅ SEED AUDIT LOGS pour les notifications du dashboard
        await SeedAuditLogsAsync(context, testTenant.Id, testUser.Id, logger);

        logger.LogInformation("Development data seeded successfully");
    }

    // ════════════════════════════════════════════════════════════════
    // SEED CLIENTS avec Bogus
    // ════════════════════════════════════════════════════════════════

    private static async Task SeedClientsAsync(
        ApplicationDbContext context,
        int tenantId,
        string userId,
        ILogger logger)
    {
        logger.LogInformation("Seeding clients with Bogus...");

        Randomizer.Seed = new Random(123); // Seed fixe pour reproductibilité

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

        // Ajouter des adresses pour chaque client
        var addressFaker = new Faker<ClientAddress>("fr")
            .RuleFor(ca => ca.Label, f => f.PickRandom("Domicile", "Bureau", "Entrepôt", "Résidence secondaire"))
            .RuleFor(ca => ca.Address, f => f.Address.StreetAddress())
            .RuleFor(ca => ca.ZipCode, f => f.Address.ZipCode("7####"))
            .RuleFor(ca => ca.City, f => f.PickRandom(
                "Paris", "Reims", "Épernay", "Châlons-en-Champagne", "Troyes",
                "Vitry-le-François", "Saint-Dizier", "Romilly-sur-Seine", "Sainte-Savine", "La Chapelle-Saint-Luc"
            ))
            .RuleFor(ca => ca.Latitude, f => f.Address.Latitude())
            .RuleFor(ca => ca.Longitude, f => f.Address.Longitude())
            .RuleFor(ca => ca.Complement, f => f.Random.Bool(0.3f) ? f.Address.SecondaryAddress() : null)
            .RuleFor(ca => ca.IsDefault, (f, ca) => true); // La première sera par défaut

        foreach (var client in clients)
        {
            var addresses = addressFaker
                .RuleFor(ca => ca.ClientId, _ => client.Id)
                .Generate(new Faker().Random.Int(1, 2)); // 1 ou 2 adresses par client

            // Assurer qu'une seule adresse est par défaut
            if (addresses.Count > 1)
            {
                addresses[1].IsDefault = false;
            }

            context.ClientAddresses.AddRange(addresses);
        }

        await context.SaveChangesAsync();

        logger.LogInformation("✅ Seeded 20 clients with addresses");
    }

    // ════════════════════════════════════════════════════════════════
    // SEED DELIVERIES avec Bogus
    // ════════════════════════════════════════════════════════════════

    private static async Task SeedDeliveriesAsync(
        ApplicationDbContext context,
        int tenantId,
        string userId,
        Store[] stores,
        ILogger logger)
    {
        logger.LogInformation("Seeding deliveries with Bogus...");

        var clients = await context.Clients
            .Include(c => c.Addresses)
            .Where(c => c.TenantId == tenantId)
            .ToListAsync();

        if (!clients.Any())
        {
            logger.LogWarning("No clients found, skipping deliveries seed");
            return;
        }

        Randomizer.Seed = new Random(456);

        var sequentialNumber = 5000;
        var deliveries = new List<Delivery>();

        // ═══ À PLANIFIER (10 livraisons) ═══
        deliveries.AddRange(GenerateDeliveries(
            count: 10,
            status: DeliveryStatus.ToBePlanned,
            tenantId: tenantId,
            userId: userId,
            clients: clients,
            stores: stores,
            sequentialNumber: ref sequentialNumber,
            dateRange: (1, 30) // Futures
        ));

        // ═══ PLANIFIÉES (15 livraisons dont 5 aujourd'hui) ═══
        deliveries.AddRange(GenerateDeliveries(
            count: 10,
            status: DeliveryStatus.Confirmed,
            tenantId: tenantId,
            userId: userId,
            clients: clients,
            stores: stores,
            sequentialNumber: ref sequentialNumber,
            dateRange: (1, 20) // Futures
        ));

        deliveries.AddRange(GenerateDeliveries(
            count: 5,
            status: DeliveryStatus.Confirmed,
            tenantId: tenantId,
            userId: userId,
            clients: clients,
            stores: stores,
            sequentialNumber: ref sequentialNumber,
            dateRange: (0, 0) // Aujourd'hui
        ));

        // ═══ EN COURS (8 livraisons) ═══
        deliveries.AddRange(GenerateDeliveries(
            count: 8,
            status: DeliveryStatus.InProgress,
            tenantId: tenantId,
            userId: userId,
            clients: clients,
            stores: stores,
            sequentialNumber: ref sequentialNumber,
            dateRange: (-2, 0) // Hier ou aujourd'hui
        ));

        // ═══ LIVRÉES (20 livraisons) ═══
        deliveries.AddRange(GenerateDeliveries(
            count: 20,
            status: DeliveryStatus.Delivered,
            tenantId: tenantId,
            userId: userId,
            clients: clients,
            stores: stores,
            sequentialNumber: ref sequentialNumber,
            dateRange: (-30, -1) // Passées
        ));

        // ═══ LIVRÉES AUJOURD'HUI (5 livraisons) ═══
        deliveries.AddRange(GenerateDeliveries(
            count: 5,
            status: DeliveryStatus.Delivered,
            tenantId: tenantId,
            userId: userId,
            clients: clients,
            stores: stores,
            sequentialNumber: ref sequentialNumber,
            dateRange: (0, 0) // Aujourd'hui
        ));

        // ═══ EN COURS AUJOURD'HUI (3 livraisons) ═══
        deliveries.AddRange(GenerateDeliveries(
            count: 3,
            status: DeliveryStatus.InProgress,
            tenantId: tenantId,
            userId: userId,
            clients: clients,
            stores: stores,
            sequentialNumber: ref sequentialNumber,
            dateRange: (0, 0) // Aujourd'hui
        ));

        // ═══ ANNULÉES (5 livraisons) ═══
        deliveries.AddRange(GenerateDeliveries(
            count: 5,
            status: DeliveryStatus.Canceled,
            tenantId: tenantId,
            userId: userId,
            clients: clients,
            stores: stores,
            sequentialNumber: ref sequentialNumber,
            dateRange: (-15, 5) // Mixte
        ));

        context.Deliveries.AddRange(deliveries);
        await context.SaveChangesAsync();

        // ═══ ITEMS pour chaque livraison ═══
        await SeedDeliveryItemsAsync(context, deliveries, logger);

        logger.LogInformation("✅ Seeded {Count} deliveries", deliveries.Count);
    }

    private static List<Delivery> GenerateDeliveries(
        int count,
        DeliveryStatus status,
        int tenantId,
        string userId,
        List<Client> clients,
        Store[] stores,
        ref int sequentialNumber,
        (int min, int max) dateRange)
    {
        var faker = new Faker("fr");
        var deliveries = new List<Delivery>();

        for (int i = 0; i < count; i++)
        {
            var client = faker.PickRandom(clients);
            var address = client.Addresses.First(a => a.IsDefault);
            var store = faker.PickRandom(stores);

            var scheduledDate = dateRange.min == 0 && dateRange.max == 0
                ? DateTime.Today
                : DateTime.Today.AddDays(faker.Random.Int(dateRange.min, dateRange.max));

            var delivery = new Delivery
            {
                TenantId = tenantId,
                SequentialNumber = sequentialNumber++,
                Reference = $"DL-{DateTime.Now.Year}-{sequentialNumber:D4}",
                Status = status,

                // Client
                ClientId = client.Id,

                // Address
                ClientAddressId = address.Id,

                // Store
                StoreId = store.Id,

                // Details
                FileNumber = faker.Random.Bool(0.7f)
                    ? $"D-{faker.Random.Int(1000, 9999)}"
                    : $"D-{faker.Random.Int(1000, 9999)}",
                ScheduledDate = scheduledDate,
                Price = faker.Random.Decimal(150, 1500),
                ClientPaymentAmount = faker.Random.Decimal(50, 500),
                StorePaymentAmount = faker.Random.Decimal(100, 800),

                WithAssembly = faker.Random.Bool(0.3f),
                DeliveryNotes = faker.Random.Bool(0.4f)
                    ? faker.PickRandom(
                        "Sonner 2 fois",
                        "Livrer à l'arrière du bâtiment",
                        "Code portail: 1234",
                        "Appeler avant",
                        "Déposer devant la porte"
                    )
                    : null,
                InternalNotes = faker.Random.Bool(0.2f)
                    ? faker.PickRandom(
                        "Client difficile",
                        "Fragile",
                        "Prioritaire",
                        "Attention chien"
                    )
                    : null,

                // Audit
                CreatedDate = DateTime.UtcNow.AddDays(faker.Random.Int(-30, 0)),
                CreatedBy = userId,
                ModifiedDate = DateTime.UtcNow,
                ModifiedBy = userId,
            };

            deliveries.Add(delivery);
        }

        return deliveries;
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
            var itemCount = faker.Random.Int(1, 4); // 1 à 4 articles par livraison

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

        logger.LogInformation("✅ Seeded {Count} delivery items", items.Count);
    }

    // ════════════════════════════════════════════════════════════════
// SEED VEHICLES, DRIVERS, ROUTESHEETS
// ════════════════════════════════════════════════════════════════

    private static async Task SeedVehiclesAndDriversAsync(
        ApplicationDbContext context,
        int tenantId,
        string userId,
        ILogger logger)
    {
        logger.LogInformation("Seeding vehicles and drivers...");

        var faker = new Faker("fr");

        // Créer 3 véhicules
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

        // Affecter TenantId via réflexion (setters privés)
        foreach (var vehicle in vehicles)
        {
            typeof(Vehicle).GetProperty("TenantId")!
                .SetValue(vehicle, tenantId);
        }

        context.Vehicles.AddRange(vehicles);
        await context.SaveChangesAsync();

        logger.LogInformation("✅ Seeded {Count} vehicles", vehicles.Length);

        // Créer utilisateurs pour drivers
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

        // Créer drivers
        var drivers = new List<Driver>();

        for (int i = 0; i < createdDriverUsers.Count; i++)
        {
            var driver = Driver.Create(
                userId: createdDriverUsers[i].Id,
                licenseNumber: $"LIC{faker.Random.Int(100000, 999999)}",
                licenseExpiryDate: DateTime.Today.AddYears(faker.Random.Int(1, 5))
            );

            typeof(Driver).GetProperty("TenantId")!
                .SetValue(driver, tenantId);

            drivers.Add(driver);
        }

        context.Drivers.AddRange(drivers);
        await context.SaveChangesAsync();

        logger.LogInformation("✅ Seeded {Count} drivers", drivers.Count);
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

        // Vérifier si des dépôts existent déjà pour ce tenant
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
                isDefault: true  // Premier dépôt = par défaut
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

        logger.LogInformation("✅ Seeded {Count} tenant depots", depots.Length);
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

        // Étaler les timestamps sur les dernières heures via réflexion
        var timestampProp = typeof(AuditLog).GetProperty("Timestamp")!;
        for (int i = 0; i < logs.Length; i++)
        {
            timestampProp.SetValue(logs[i], now.AddMinutes(-(i * 25)));
        }

        context.AuditLogs.AddRange(logs);
        await context.SaveChangesAsync();

        logger.LogInformation("✅ Seeded {Count} audit logs", logs.Length);
    }
}