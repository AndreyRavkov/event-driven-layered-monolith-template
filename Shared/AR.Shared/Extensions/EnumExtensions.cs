using System.ComponentModel;

namespace AR.Shared.Extensions;

public static class EnumExtensions
{
    public static int ToIntExt<T>(this T value) where T : Enum => Convert.ToInt32(value);

    public static string GetDescriptionExt<T>(this T value) where T : Enum
    {
        var fieldInfo = value.GetType().GetField(value.ToString());

        if (fieldInfo != null)
        {
            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes?.Length > 0 ? attributes[0].Description : value.ToString()!;
        }

        return value.ToString();
    }
}