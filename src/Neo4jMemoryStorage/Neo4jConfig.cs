namespace Neo4j.KernelMemory.MemoryStorage;

public class Neo4jConfig
{
    /// <summary>
    /// Uri for connecting to Neo4j.
    /// Default is "neo4j://localhost:7687"
    /// </summary>
    public string Uri { get; set; } = "neo4j://localhost:7687";

    /// <summary>
    /// Username required to connect to Neo4j. 
    /// Dedault is "neo4j"
    /// </summary>
    public string Username { get; set; } = "neo4j";

    /// <summary>
    /// Password for authenticating username with Neo4j.
    /// </summary>
    public string Password { get; set; } = string.Empty;

}
