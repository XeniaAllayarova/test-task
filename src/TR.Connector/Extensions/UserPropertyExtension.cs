using TR.Connectors.Api.Entities;

namespace TR.Connector.Extensions
{
    internal static class UserPropertyExtension
    {
        public static string GetPropertyValue(this IEnumerable<UserProperty> userProperties, string propertyName)
        {
            return userProperties.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        }

        public static bool GetBoolPropertyValue(this IEnumerable<UserProperty> userProperties, string propertyName, bool defValue = false)
        {
            var value = userProperties.GetPropertyValue(propertyName);

            return bool.TryParse(value, out bool result) ? result : defValue;
        }
    }
}
