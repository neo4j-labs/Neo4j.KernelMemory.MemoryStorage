using Microsoft.Extensions.DependencyInjection;
using Neo4j.KernelMemory.MemoryStorage;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

/// <summary>
///     Extensions for KernelMemoryBuilder
/// </summary>
public static class KernelMemoryBuilderExtensions
{
    /// <summary>
    ///     Kernel Memory Builder extension method to add the Neo4j memory connector.
    /// </summary>
    /// <param name="builder">The IKernelMemoryBuilder instance.</param>
    /// <param name="configuration">The application configuration.</param>
    public static IKernelMemoryBuilder WithNeo4j(this IKernelMemoryBuilder builder,
        Neo4jConfig configuration)
    {
        builder.Services.AddNeo4jAsVectorDb(configuration);

        return builder;
    }
}