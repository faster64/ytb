using System.ComponentModel;

namespace Ytb.Extensions
{
    public static class EnumerationExtensions
    {
        public static string GetDescription(this Enum en)
        {
            var fi = en.GetType().GetField(en.ToString());
            if (fi == null)
            {
                return string.Empty;
            }

            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes.Any())
            {
                return attributes.First().Description;
            }
            return en.ToString();
        }
    }
}
