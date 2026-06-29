using System.Globalization;

namespace Router.Core.Helpers;

internal static class TypeConverters
{
    private static readonly Dictionary<string, Type> TypesByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["string"]  = typeof(string),
        ["str"]     = typeof(string),
        ["int"]     = typeof(int),
        ["int32"]   = typeof(int),
        ["long"]    = typeof(long),
        ["int64"]   = typeof(long),
        ["short"]   = typeof(short),
        ["int16"]   = typeof(short),
        ["byte"]    = typeof(byte),
        ["float"]   = typeof(float),
        ["single"]  = typeof(float),
        ["double"]  = typeof(double),
        ["decimal"] = typeof(decimal),
        ["bool"]    = typeof(bool),
        ["boolean"] = typeof(bool),
        ["guid"]    = typeof(Guid),
        ["datetime"] = typeof(DateTime),
        ["date"]    = typeof(DateTime),
    };

    public static Type ResolveType(string name)
    {
        if (TypesByName.TryGetValue(name, out var t)) return t;
            throw new NotSupportedException($"Type '{name}' is not supported.");
    }

    public static bool TryConvert(string value, Type targetType, out object? result)
    {
        try
        {
            result = ConvertTo(value, targetType);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    public static object? ConvertTo(string value, Type targetType)
    {
        if (targetType == typeof(string)) return value;
        if (targetType == typeof(int))   return int.Parse(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(long))  return long.Parse(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(short)) return short.Parse(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(byte))  return byte.Parse(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(float)) return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        if (targetType == typeof(double))return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        if (targetType == typeof(decimal))return decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        if (targetType == typeof(bool))  return bool.Parse(value);
        if (targetType == typeof(Guid))  return Guid.Parse(value);
        if (targetType == typeof(DateTime))
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}