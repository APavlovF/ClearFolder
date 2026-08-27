namespace ClearFolder;

public static class Extensions
{
    public static string TrimLongString(this string name, int maxLength)
    {
        if (string.IsNullOrEmpty(name) || name.Length <= maxLength) return name;
        int keep = (maxLength - 1) / 2;
        string start = name[..keep];
        string end = name[^keep..];

        return $"{start}~{end}";
    }
}