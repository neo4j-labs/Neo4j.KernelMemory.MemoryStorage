using System;

using Microsoft.KernelMemory;

namespace Neo4j.KernelMemory.MemoryStorage;

public class Neo4jException : KernelMemoryException
{
    /// <inheritdoc />
    public Neo4jException() { }

    /// <inheritdoc />
    public Neo4jException(string message) : base(message) { }

    /// <inheritdoc />
    public Neo4jException(string message, Exception? innerException) : base(message, innerException) { }
}
