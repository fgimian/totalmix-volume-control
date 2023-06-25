namespace TotalMixVC.Configuration.NamingPolicies;

/// <summary>
/// Implements a JSON naming policy that converts names to snake case. This functionality will be
/// introduced in .NET 8 at which point I will switch to the standard library and remove this
/// implementation.
/// See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Text.Json/Common/JsonSnakeCaseLowerNamingPolicy.cs.
/// </summary>
internal sealed class JsonSnakeCaseLowerNamingPolicy : JsonSeparatorNamingPolicy
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.DocumentationRules",
        "SA1600:Elements should be documented",
        Justification = "This functionality is temporary as it will be included in .NET 8."
    )]
    public JsonSnakeCaseLowerNamingPolicy()
        : base(lowercase: true, separator: '_') { }
}
