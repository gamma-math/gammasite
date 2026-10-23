using System;

namespace GamMaSite.Services;

/*
 * Centralizes canonical paths for legacy browser URLs.
 * Keeping this mapping separate makes redirect behavior testable without a database.
 */
public static class LegacyRouteRedirects
{
    public static string BuildLocation(string path, string queryString = "")
    {
        var canonicalPath = path;

        if (string.Equals(path, "library", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "bibliotek", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "referater", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "Library/Index", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "Library/IndexAsync", StringComparison.OrdinalIgnoreCase))
        {
            canonicalPath = "Library";
        }

        var target = string.IsNullOrWhiteSpace(canonicalPath) ? "/" : $"/{canonicalPath}";
        return $"{target}{queryString}";
    }
}
