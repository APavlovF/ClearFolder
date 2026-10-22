using System;

namespace ClearFolder;

public static class Extensions
{
    public static string FormatFileSize(this long bytes)
    {
        const int unit = 1024;
        if (bytes < unit) { return $"{bytes} B"; }
        int exp = (int)(Math.Log(bytes) / Math.Log(unit));
        return $"{bytes / Math.Pow(unit, exp):F2} {"KMGTPE"[exp - 1]}B";
    }

    public static string TrimLongString(this string name, int maxLength)
    {
        if (string.IsNullOrEmpty(name) || name.Length <= maxLength) return name;
        int keep = (maxLength - 1) / 2;
        string start = name[..keep];
        string end = name[^keep..];

        return $"{start}~{end}";
    }
}