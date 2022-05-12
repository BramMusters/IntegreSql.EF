﻿using System;
using System.Linq;
using System.Net.Http;
using MccSoft.IntegreSql.EF;
using MccSoft.IntegreSql.EF.DatabaseInitialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleWeb;

public class IntegrationTestBaseWithoutEnsureCreated
{
    protected readonly HttpClient _httpClient;
    private readonly IDatabaseInitializer _databaseInitializer;

    protected IntegrationTestBaseWithoutEnsureCreated(DatabaseType databaseType)
    {
        _databaseInitializer = CreateDatabaseInitializer(databaseType);
        var connectionString = _databaseInitializer.CreateDatabaseGetConnectionStringSync(
            new BasicDatabaseSeedingOptions<ExampleDbContext>(
                Name: "IntegrationWithoutEnsureCreated",
                DisableEnsureCreated: true
            )
        );

        var webAppFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(
            builder =>
            {
                builder.ConfigureServices(
                    services =>
                    {
                        var descriptor = services.Single(
                            d => d.ServiceType == typeof(DbContextOptions<ExampleDbContext>)
                        );
                        services.Remove(descriptor);

                        services.AddDbContext<ExampleDbContext>(
                            options => _databaseInitializer.UseProvider(options, connectionString)
                        );
                    }
                );
            }
        );

        _httpClient = webAppFactory.CreateDefaultClient();
    }

    private IDatabaseInitializer CreateDatabaseInitializer(DatabaseType databaseType)
    {
        // this is needed if you run tests NOT inside the container
        NpgsqlDatabaseInitializer.ConnectionStringOverride = new ConnectionStringOverride()
        {
            Host = "localhost",
            Port = 5434,
        };
        return databaseType switch
        {
            DatabaseType.Postgres => new NpgsqlDatabaseInitializer(),
            DatabaseType.Sqlite => new SqliteDatabaseInitializer(),
            _ => throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, null)
        };
    }
}
