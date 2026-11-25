using AR.NetExt.Utils.Collections;

namespace AR.Shared.Extensions;

public static class StringExtensions
{
    public static bool IsFileExtensionValid(this string fileName, string allowedExtensions)
        => fileName.IsFileExtensionValid(ParseAllowedExtensionsString(allowedExtensions));

    public static bool IsFileExtensionValid(this string fileName, ICollection<string> allowedExtensions)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }
        var fileExtension = Path.GetExtension(fileName).Replace(".", "").Trim().ToUpperInvariant();
        return allowedExtensions.Contains(fileExtension);
    }

    private static List<string> ParseAllowedExtensionsString(string allowedExtensions)
        => allowedExtensions.Split(',').Select(x => x.Trim().ToUpperInvariant()).AsListExt();
}
