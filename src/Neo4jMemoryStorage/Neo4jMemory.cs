using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;

using Neo4j.Driver;

namespace Neo4j.KernelMemory.MemoryStorage;

/// <summary>
/// Basic vector db implementation, designed for tests and demos only.
/// When searching, uses brute force comparing against all stored records.
/// </summary>
public class Neo4jMemory : IMemoryDb
{
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly ILogger<Neo4jMemory> _log;

    private readonly IDriver _driver;

    /// <summary>
    /// Create new instance
    /// </summary>
    /// <param name="config">Neo4j connection settings</param>
    /// <param name="embeddingGenerator">Text embedding generator</param>
    /// <param name="log">Application logger</param>
    public Neo4jMemory(
        Neo4jConfig config,
        ITextEmbeddingGenerator embeddingGenerator,
        ILogger<Neo4jMemory>? log = null)
    {
        this._embeddingGenerator = embeddingGenerator;

        if (this._embeddingGenerator == null)
        {
            throw new Neo4jException("Embedding generator not configured");
        }

        this._log = log ?? DefaultLogger<Neo4jMemory>.Instance;

        this._driver = Neo4jDriverFactory.BuildDriver(config, this._log);

        Console.WriteLine($"Neo4jMemory created for {config.Uri}");
    }

    /// <inheritdoc />
    public Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);
        var label = LabelForIndex(index);
        var propertyKey = PropertyKeyForIndex(index);

        // Create a new index in Neo4j
        return this._driver.ExecutableQuery($$"""
            CREATE VECTOR INDEX `{{index}}` IF NOT EXISTS
                FOR (m:{{label}}) ON (m.{{propertyKey}}) 
                OPTIONS { indexConfig: {
                    `vector.dimensions`: $vectorSize,
                    `vector.similarity_function`: 'cosine'
                    }
                }
            """
        )
        .WithParameters(new { vectorSize }).ExecuteAsync(cancellationToken);

    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        var response = await this._driver.ExecutableQuery($"SHOW VECTOR INDEXES YIELD name")
            .ExecuteAsync(cancellationToken).ConfigureAwait(false);

        var vectorIndexes = response.Result.ToList().Select(x => x["name"].As<string>());
        return vectorIndexes;
    }

    /// <inheritdoc />
    public Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);
        var label = LabelForIndex(index);
        var propertyKey = PropertyKeyForIndex(index);
        // Create a new index in Neo4j
        return this._driver.ExecutableQuery($$"""
            CREATE VECTOR INDEX `{{index}}` IF NOT EXISTS
                FOR (c:{{label}}) ON (c.{{propertyKey}}) 
                OPTIONS { indexConfig: {
                    `vector.dimensions`: $vectorSize,
                    `vector.similarity_function`: 'cosine'
                    }
                }
            """
        )
        .ExecuteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);
        var label = LabelForIndex(index);
        var propertyKey = PropertyKeyForIndex(index);

        var response = await this._driver
            .ExecutableQuery($$"""
                MERGE (m:Memory:{{label}} {id: $recordId}) 
                SET m += $payload
                WITH m
                CALL db.create.setNodeVectorProperty(m, $propertyKey, $vector)  
                RETURN m.id as recordId
            """)
            .WithParameters(new { 
                recordId = record.Id, 
                payload = record.Payload,
                propertyKey = propertyKey,
                vector = record.Vector.Data.ToArray()
            })
            .ExecuteAsync(cancellationToken).ConfigureAwait(false);

        var recordId = response.Result.Single()["recordId"].As<string>();

        return recordId;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string index,
        string text,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (limit <= 0) { limit = int.MaxValue; }

        index = NormalizeIndexName(index);
        var propertyKey = PropertyKeyForIndex(index);

        var vector = await _embeddingGenerator.GenerateEmbeddingAsync(text)
                                                            .ConfigureAwait(false);
        
        var queryResult = await this._driver
            .ExecutableQuery($$"""
            CALL db.index.vector.queryNodes($indexName, $topK, $vector) 
            YIELD node, score
            RETURN score, node.id as recordId, properties(node) as payload, node[$propertyKey] as vector
        """)
            .WithParameters(new { 
                indexName = index,
                topK = limit,
                propertyKey = propertyKey,
                vector = vector.Data.ToArray()
            })
            .ExecuteAsync(cancellationToken).ConfigureAwait(false);

        var memories = queryResult.Result.Select(memory => (
            new MemoryRecord()
                {
                    Id = memory["recordId"].As<string>(),
                    Payload = memory["payload"].As<Dictionary<string, object>>(),
                    Vector = memory["vector"].As<List<float>>().ToArray()
                },
            memory["score"].As<double>()
            )
        )
        .ToList();

        foreach (var memory in memories)
        {
            yield return memory;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MemoryRecord> GetListAsync(
        string index,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (limit <= 0) { limit = int.MaxValue; }

        index = NormalizeIndexName(index);

        // Get similar records from Neo4j
        await Task.Delay(0, cancellationToken).ConfigureAwait(false);

        yield return (new MemoryRecord());
    }

    /// <inheritdoc />
    public Task DeleteAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        return Task.CompletedTask;
    }

    #region private

    // Note: normalize "_" to "-" for consistency with other DBs
    private static readonly Regex s_replaceIndexNameCharsRegex = new(@"[\s|\\|/|.|_|:]");
    private const string ValidSeparator = "-";

    private static string NormalizeIndexName(string index)
    {
        if (string.IsNullOrWhiteSpace(index))
        {
            throw new ArgumentNullException(nameof(index), "The index name is empty");
        }

        index = s_replaceIndexNameCharsRegex.Replace(index.Trim().ToLowerInvariant(), ValidSeparator);

        return index.Trim();
    }

    private static string LabelForIndex(string index)
    {
        // ABKNOTE: return index as TitleCase would be better
        return index.ToUpperInvariant();
    }

    private static string PropertyKeyForIndex(string index)
    {
        return index.ToLowerInvariant() + "Embedding";
    }

    private static bool TagsMatchFilters(TagCollection tags, ICollection<MemoryFilter>? filters)
    {
        if (filters == null || filters.Count == 0) { return true; }

        // Verify that at least one filter matches (OR logic)
        foreach (MemoryFilter filter in filters)
        {
            var match = true;

            // Verify that all conditions are met (AND logic)
            foreach (KeyValuePair<string, List<string?>> condition in filter)
            {
                // Check if the tag name + value is present
                for (int index = 0; match && index < condition.Value.Count; index++)
                {
                    match = match && (tags.ContainsKey(condition.Key) && tags[condition.Key].Contains(condition.Value[index]));
                }
            }

            if (match) { return true; }
        }

        return false;
    }

    private static string EncodeId(string realId)
    {
        var bytes = Encoding.UTF8.GetBytes(realId);
        return Convert.ToBase64String(bytes).Replace('=', '_');
    }

    private static string DecodeId(string encodedId)
    {
        var bytes = Convert.FromBase64String(encodedId.Replace('_', '='));
        return Encoding.UTF8.GetString(bytes);
    }

    #endregion
}
