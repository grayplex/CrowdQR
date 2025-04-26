using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CrowdQR.Web.Utilities;

/// <summary>
/// Helper class for generating URL-friendly slugs.
/// </summary>
public static partial class SlugHelper
{
    /// <summary>
    /// Generates a URL-friendly slug from text.
    /// </summary>
    /// <param name="text">The text to convert to a slug.</param>
    /// <returns>A URL-friendly slug.</returns>
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Convert to lowercase
        var slug = text.ToLowerInvariant();

        // Remove accents
        slug = RemoveDiacritics(slug);

        // Replace spaces and punctuation with hyphens
        slug = InvalidCharacterRegex().Replace(slug, "");
        slug = WhitespaceRegex().Replace(slug, "-");

        // Remove consecutive hyphens
        slug = ConsecutiveHyphenRegex().Replace(slug, "-");

        // Remove leading and trailing hyphens
        slug = slug.Trim('-');

        return slug;
    }

    /// <summary>
    /// Removes diacritics (accents) from text.
    /// </summary>
    /// <param name="text">The text to remove diacritics from.</param>
    /// <returns>Text without diacritics.</returns>
    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex InvalidCharacterRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"-+")]
    private static partial Regex ConsecutiveHyphenRegex();
}