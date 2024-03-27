using Microsoft.KernelMemory.MemoryStorage;
using Neo4j.KernelMemory.MemoryStorage;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Extensions for KernelMemoryBuilder and generic DI
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Inject Neo4j as the default implementation of IMemoryDb
    /// </summary>
    public static IServiceCollection AddNeo4jAsVectorDb(this IServiceCollection services, Neo4jConfig neo4jConfig)
    {
        ArgumentNullException.ThrowIfNull(neo4jConfig);

        services.AddSingleton(sp =>
        {
            var neo4jConfig = sp.GetRequiredService<Neo4jConfig>();
            return Neo4jDriverFactory.BuildDriver(neo4jConfig, sp.GetRequiredService<ILogger>());
        });

        return services
            .AddSingleton(neo4jConfig)
            .AddSingleton<IMemoryDb, Neo4jMemory>();
    }
}