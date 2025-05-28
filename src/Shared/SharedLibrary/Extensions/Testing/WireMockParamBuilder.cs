using WireMock.Admin.Mappings;

namespace SharedLibrary.Extensions.Testing;

/// <summary>
/// Utility methods for creating WireMock parameter models with various matchers.
/// </summary>
public static class WireMockParamBuilder
{
    /// <summary>
    /// Creates a ParamModel with an ExactMatcher.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="value">Exact value to match.</param>
    /// <returns>A configured ParamModel.</returns>
    public static ParamModel WithExactMatch(string name, string value) =>
        new()
        {
            Name = name,
            Matchers = [new MatcherModel { Name = "ExactMatcher", Pattern = value }],
        };

    /// <summary>
    /// Creates a ParamModel with a RegexMatcher.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="pattern">Regex pattern to match.</param>
    /// <returns>A configured ParamModel.</returns>
    public static ParamModel WithRegexMatch(string name, string pattern) =>
        new()
        {
            Name = name,
            Matchers = [new MatcherModel { Name = "RegexMatcher", Pattern = pattern }],
        };

    /// <summary>
    /// Creates a ParamModel with a WildcardMatcher.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="pattern">Wildcard pattern to match.</param>
    /// <returns>A configured ParamModel.</returns>
    public static ParamModel WithWildcardMatch(string name, string pattern) =>
        new()
        {
            Name = name,
            Matchers = [new MatcherModel { Name = "WildcardMatcher", Pattern = pattern }],
        };

    /// <summary>
    /// Creates a ParamModel with a NotNullOrEmptyMatcher.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <returns>A configured ParamModel.</returns>
    public static ParamModel WithNotNullOrEmptyMatch(string name) =>
        new() { Name = name, Matchers = [new MatcherModel { Name = "NotNullOrEmptyMatcher" }] };

    /// <summary>
    /// Creates a ParamModel with a JsonMatcher.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="jsonPattern">JSON pattern to match.</param>
    /// <returns>A configured ParamModel.</returns>
    public static ParamModel WithJsonMatch(string name, string jsonPattern) =>
        new()
        {
            Name = name,
            Matchers = [new MatcherModel { Name = "JsonMatcher", Pattern = jsonPattern }],
        };

    /// <summary>
    /// Creates a ParamModel with a JsonPathMatcher.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="jsonPathPattern">JSONPath expression to match.</param>
    /// <returns>A configured ParamModel.</returns>
    public static ParamModel WithJsonPathMatch(string name, string jsonPathPattern) =>
        new()
        {
            Name = name,
            Matchers = [new MatcherModel { Name = "JsonPathMatcher", Pattern = jsonPathPattern }],
        };

    /// <summary>
    /// Creates a ParamModel with a LinqMatcher.
    /// Note: This is a placeholder as LinqMatcher requires LINQ expressions that can't
    /// be serialized directly in WireMock.Admin.Mappings.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="linqPattern">LINQ expression pattern.</param>
    /// <returns>A configured ParamModel.</returns>
    public static ParamModel WithLinqMatch(string name, string linqPattern) =>
        new()
        {
            Name = name,
            Matchers = [new MatcherModel { Name = "LinqMatcher", Pattern = linqPattern }],
        };

    /// <summary>
    /// Creates a ParamModel with a XPathMatcher.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="xpathPattern">XPath expression to match.</param>
    /// <returns>A configured ParamModel.</returns>
    public static ParamModel WithXPathMatch(string name, string xpathPattern) =>
        new()
        {
            Name = name,
            Matchers = [new MatcherModel { Name = "XPathMatcher", Pattern = xpathPattern }],
        };

    /// <summary>
    /// Creates a ParamModel with a JmesPathMatcher.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="jmesPathPattern">JMESPath expression to match.</param>
    /// <returns>A configured ParamModel.</returns>
    public static ParamModel WithJmesPathMatch(string name, string jmesPathPattern) =>
        new()
        {
            Name = name,
            Matchers = [new MatcherModel { Name = "JmesPathMatcher", Pattern = jmesPathPattern }],
        };

    /// <summary>
    /// Creates a ParamModel with a GraphQLMatcher.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="graphQlPattern">GraphQL query to match.</param>
    /// <returns>A configured ParamModel.</returns>
    public static ParamModel WithGraphQLMatch(string name, string graphQlPattern) =>
        new()
        {
            Name = name,
            Matchers = [new MatcherModel { Name = "GraphQLMatcher", Pattern = graphQlPattern }],
        };

    /// <summary>
    /// Creates a ParamModel with a CSharpCodeMatcher.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="csharpPattern">C# code pattern to match.</param>
    /// <returns>A configured ParamModel.</returns>
    public static ParamModel WithCSharpCodeMatch(string name, string csharpPattern) =>
        new()
        {
            Name = name,
            Matchers = [new MatcherModel { Name = "CSharpCodeMatcher", Pattern = csharpPattern }],
        };

    /// <summary>
    /// Creates a ParamModel with a ContentTypeMatcher.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="contentTypePattern">Content type pattern to match.</param>
    /// <returns>A configured ParamModel.</returns>
    public static ParamModel WithContentTypeMatch(string name, string contentTypePattern) =>
        new()
        {
            Name = name,
            Matchers =
            [
                new MatcherModel { Name = "ContentTypeMatcher", Pattern = contentTypePattern },
            ],
        };
}
