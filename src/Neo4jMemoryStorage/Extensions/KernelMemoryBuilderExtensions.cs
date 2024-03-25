using Neo4j.KernelMemory.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.KernelMemory;

/// <summary>
/// Extensions for KernelMemoryBuilder
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Kernel Memory Builder extension method to add the Neo4j memory connector.
    /// </summary>
    /// <param name="builder">The IKernelMemoryBuilder instance</param>
    /// <param name="configuration">The application configuration</param>"
    public static IKernelMemoryBuilder WithNeo4j(this IKernelMemoryBuilder builder,
        Neo4jConfig configuration)
    {
        builder.Services.AddNeo4jAsVectorDb(configuration);

        return builder;
    }

}
