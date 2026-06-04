using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TerziTuran.Web.Extensions;

public static class DisplayExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
    }
}
