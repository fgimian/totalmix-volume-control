using System.Buffers;
using System.Globalization;
using System.Text.Json;

namespace TotalMixVC.Configuration.NamingPolicies;

/// <summary>
/// Implements an abstract JSON separator naming policy which can be used to create other naming
/// policies. This functionality will be introduced in .NET 8 at which point I will switch to the
/// standard library and remove this implementation.
/// See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Text.Json/Common/JsonSeparatorNamingPolicy.cs.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1600:Elements should be documented",
    Justification = "This functionality is temporary as it will be included in .NET 8."
)]
internal abstract class JsonSeparatorNamingPolicy : JsonNamingPolicy
{
    public const int StackallocByteThreshold = 256;

    public const int StackallocCharThreshold = StackallocByteThreshold / 2;

    private readonly bool _lowercase;

    private readonly char _separator;

    protected JsonSeparatorNamingPolicy(bool lowercase, char separator) =>
        (_lowercase, _separator) = (lowercase, separator);

    private enum CharCategory
    {
        Boundary,

        Lowercase,

        Uppercase,
    }

    public sealed override string ConvertName(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        // Rented buffer 20% longer that the input.
        int rentedBufferLength = (12 * name.Length) / 10;
        char[]? rentedBuffer =
            rentedBufferLength > StackallocCharThreshold
                ? ArrayPool<char>.Shared.Rent(rentedBufferLength)
                : null;

        int resultUsedLength = 0;
        Span<char> result = rentedBuffer ?? stackalloc char[StackallocCharThreshold];

        void ExpandBuffer(ref Span<char> result)
        {
            char[] newBuffer = ArrayPool<char>.Shared.Rent(result.Length * 2);

            result.CopyTo(newBuffer);

            if (rentedBuffer is not null)
            {
                result[..resultUsedLength].Clear();
                ArrayPool<char>.Shared.Return(rentedBuffer);
            }

            rentedBuffer = newBuffer;
            result = rentedBuffer;
        }

        void WriteWord(ReadOnlySpan<char> word, ref Span<char> result)
        {
            if (word.IsEmpty)
            {
                return;
            }

            int written;
            while (true)
            {
                int destinationOffset =
                    resultUsedLength != 0 ? resultUsedLength + 1 : resultUsedLength;

                if (destinationOffset < result.Length)
                {
                    Span<char> destination = result[destinationOffset..];

                    written = _lowercase
                        ? word.ToLowerInvariant(destination)
                        : word.ToUpperInvariant(destination);

                    if (written > 0)
                    {
                        break;
                    }
                }

                ExpandBuffer(ref result);
            }

            if (resultUsedLength != 0)
            {
                result[resultUsedLength] = _separator;
                resultUsedLength++;
            }

            resultUsedLength += written;
        }

        int first = 0;
        ReadOnlySpan<char> chars = name.AsSpan();
        CharCategory previousCategory = CharCategory.Boundary;

        for (int index = 0; index < chars.Length; index++)
        {
            char current = chars[index];
            UnicodeCategory currentCategoryUnicode = char.GetUnicodeCategory(current);

            if (
                currentCategoryUnicode == UnicodeCategory.SpaceSeparator
                || (
                    currentCategoryUnicode >= UnicodeCategory.ConnectorPunctuation
                    && currentCategoryUnicode <= UnicodeCategory.OtherPunctuation
                )
            )
            {
                WriteWord(chars[first..index], ref result);

                previousCategory = CharCategory.Boundary;
                first = index + 1;

                continue;
            }

            if (index + 1 < chars.Length)
            {
                char next = chars[index + 1];
                CharCategory currentCategory = currentCategoryUnicode switch
                {
                    UnicodeCategory.LowercaseLetter => CharCategory.Lowercase,
                    UnicodeCategory.UppercaseLetter => CharCategory.Uppercase,
                    _ => previousCategory,
                };

                if (
                    (currentCategory == CharCategory.Lowercase && char.IsUpper(next)) || next == '_'
                )
                {
                    WriteWord(chars.Slice(first, index - first + 1), ref result);

                    previousCategory = CharCategory.Boundary;
                    first = index + 1;

                    continue;
                }

                if (
                    previousCategory == CharCategory.Uppercase
                    && currentCategoryUnicode == UnicodeCategory.UppercaseLetter
                    && char.IsLower(next)
                )
                {
                    WriteWord(chars[first..index], ref result);

                    previousCategory = CharCategory.Boundary;
                    first = index;

                    continue;
                }

                previousCategory = currentCategory;
            }
        }

        WriteWord(chars[first..], ref result);

        name = result[..resultUsedLength].ToString();

        if (rentedBuffer is not null)
        {
            result[..resultUsedLength].Clear();
            ArrayPool<char>.Shared.Return(rentedBuffer);
        }

        return name;
    }
}
