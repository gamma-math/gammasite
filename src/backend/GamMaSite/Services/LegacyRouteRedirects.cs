using System;

namespace GamMaSite.Services;

/*
 * Centralizes canonical paths for legacy browser URLs.
 * Keeping this mapping separate makes redirect behavior testable without a database.
 */
public static class LegacyRouteRedirects
{
    public static bool IsNonCanonicalLibraryPath(string path)
    {
        return !string.Equals(path, "/Library", StringComparison.Ordinal)
            && string.Equals(path?.Trim('/'), "library", StringComparison.OrdinalIgnoreCase);
    }

    public static string BuildLocation(string path, string queryString = "")
    {
        var normalizedPath = (path ?? string.Empty).Trim('/');
        var canonicalPath = normalizedPath;

        if (string.Equals(normalizedPath, "library", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedPath, "bibliotek", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedPath, "referater", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedPath, "Library/Index", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedPath, "Library/IndexAsync", StringComparison.OrdinalIgnoreCase))
        {
            canonicalPath = "Library";
        }

        var target = string.IsNullOrWhiteSpace(canonicalPath) ? "/" : $"/{canonicalPath}";
        return $"{target}{queryString}";
    }
}
