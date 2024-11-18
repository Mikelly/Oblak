using System.Globalization;
using System.Text;

namespace Oblak.Helpers
{
    public static class StringExtensions
    {
        public static string ToAscii(this string input)
        {
            // Normalize the string to decompose accents and special characters
            string normalizedString = input.Normalize(NormalizationForm.FormD);

            // Remove non-ASCII characters by filtering out those with Unicode category "NonSpacingMark"
            string asciiString = new string(
                normalizedString
                    .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    .ToArray());

            // Encode to ASCII to remove any remaining non-ASCII characters
            byte[] asciiBytes = Encoding.ASCII.GetBytes(asciiString);
            return Encoding.ASCII.GetString(asciiBytes);
        }
    }
}
