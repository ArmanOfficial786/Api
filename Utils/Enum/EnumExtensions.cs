using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace JsSampleReport.Utils.Enum
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this System.Enum value) // ✅ Use System.Enum, not your Enums class
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();

            if (member != null)
            {
                var displayAttr = member.GetCustomAttribute<DisplayAttribute>();

                if (displayAttr != null && !string.IsNullOrEmpty(displayAttr.Name))
                {
                    return displayAttr.Name; // ✅ Return Display attribute name
                }
            }

            return value.ToString(); // ✅ Fallback to enum member name
        }
    }
}