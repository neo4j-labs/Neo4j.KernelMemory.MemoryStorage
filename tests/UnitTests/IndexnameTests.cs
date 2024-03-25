﻿// Copyright (c) Free Mind Labs, Inc. All rights reserved.
using FreeMindLabs.KernelMemory.Elasticsearch;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests;

public class IndexnameTests
{
    private readonly ITestOutputHelper _output;
    private readonly IIndexNameHelper _indexNameHelper;

    public IndexnameTests(ITestOutputHelper output, IIndexNameHelper indexNameHelper)
    {
        this._output = output ?? throw new ArgumentNullException(nameof(output));
        this._indexNameHelper = indexNameHelper ?? throw new ArgumentNullException(nameof(indexNameHelper));
    }

    [Theory]
    [InlineData("")] // default index
    [InlineData("nondefault")]
    [InlineData("WithUppercase")]
    [InlineData("With-Dashes")]
    [InlineData("123numberfirst")]
    public void GoodIndexNamesAreAccepted(string indexName)
    {
        Assert.True(this._indexNameHelper.TryConvert(indexName, out var convResult));
        Assert.Empty(convResult.Errors);

        this._output.WriteLine($"The index name '{indexName}' will be translated to '{convResult.ActualIndexName}'.");
    }

    [Theory]
    // An index name cannot start with a hyphen (-) or underscore (_).
    //[InlineData("-test", 1)]
    //[InlineData("test_", 1)]
    // An index name can only contain letters, digits, and hyphens (-).
    [InlineData("test space", 1)]
    [InlineData("test/slash", 1)]
    [InlineData("test\\backslash", 1)]
    [InlineData("test.dot", 1)]
    [InlineData("test:colon", 1)]
    [InlineData("test*asterisk", 1)]
    [InlineData("test<less", 1)]
    [InlineData("test>greater", 1)]
    [InlineData("test|pipe", 1)]
    [InlineData("test?question", 1)]
    [InlineData("test\"quote", 1)]
    [InlineData("test'quote", 1)]
    [InlineData("test`backtick", 1)]
    [InlineData("test~tilde", 1)]
    [InlineData("test!exclamation", 1)]
    // Avoid names that are dot-only or dot and numbers
    // Multi error
    [InlineData(".", 1)]
    [InlineData("..", 1)]
    [InlineData("1.2.3", 1)]
    //[InlineData("_test", 1)]

    public void BadIndexNamesAreRejected(string indexName, int errorCount)
    {
        // Creates the index using IMemoryDb
        var exception = Assert.Throws<InvalidIndexNameException>(() =>
        {
            this._indexNameHelper.Convert(indexName);
        });

        this._output.WriteLine(
            $"The index name '{indexName}' had the following errors:\n{string.Join("\n", exception.Errors)}" +
            $"" +
            $"The expected number of errors was {errorCount}.");

        Assert.True(errorCount == exception.Errors.Count(), $"The number of errprs expected is different than the number of errors found.");
    }

    [Fact]
    public void IndexNameCannotBeLongerThan255Bytes()
    {
        var indexName = new string('a', 256);
        var exception = Assert.Throws<InvalidIndexNameException>(() =>
        {
            this._indexNameHelper.Convert(indexName);
        });

        Assert.Equal(1, exception.Errors.Count());
    }
}
