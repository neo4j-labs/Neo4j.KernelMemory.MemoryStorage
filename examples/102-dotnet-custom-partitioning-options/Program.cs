// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;
using Neo4j.Driver;
using Neo4j.KernelMemory.MemoryStorage;
using dotenv.net;

var dotEnvOptions = new DotEnvOptions(
        envFilePaths: new[] {"../.env"}
    );

DotEnv.Load(options: dotEnvOptions);
var env = DotEnv.Read(dotEnvOptions);

var neo4jConfig = new Neo4jConfig
{
    Uri = env["NEO4J_URI"],
    Username = env["NEO4J_USERNAME"],
    Password = env["NEO4J_PASSWORD"]
};

Console.WriteLine($"Neo4j URI: {neo4jConfig.Uri}");

// check that OpenAI API key is set
if (string.IsNullOrEmpty(env["OPENAI_API_KEY"]))
{
    Console.WriteLine("Please set the OPENAI_API_KEY environment variable.");
    return;
}

var kernelMemory = new KernelMemoryBuilder()
    .WithOpenAIDefaults(env["OPENAI_API_KEY"])
    .WithNeo4j(neo4jConfig)
    .WithCustomTextPartitioningOptions(new TextPartitioningOptions
    {
        // Max 99 tokens per sentence
        MaxTokensPerLine = 99,
        // When sentences are merged into paragraphs (aka partitions), stop at 299 tokens
        MaxTokensPerParagraph = 299,
        // Each paragraph contains the last 47 tokens from the previous one
        OverlappingTokens = 47,
    })
    .Build<MemoryServerless>();

await kernelMemory.ImportDocumentAsync(
    new Document().AddFile("mswordfile.docx"), 
    steps: new[]
        {
            "extract",
            "partition"
        }
    ).ConfigureAwait(true);

await kernelMemory.ImportDocumentAsync(
    new Document().AddFile("../data/deer-horatio.md")
).ConfigureAwait(true);

var question = "Tell me about Deer Horatio.";

var answer = await kernelMemory.AskAsync(question).ConfigureAwait(true);

Console.WriteLine($"Question: {question}\n\nAnswer: {answer.Result}");
