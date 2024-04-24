using System.Xml.Serialization;
using System.Xml;
using Oblak.Data;

namespace Oblak
{
    public static class Extensions
    {

        #region String Extensions

        /*
         * Extension string metode za extract substring-a 
         * po principu od-do. Uključujući indexe ili ne.
         */
        public static string SubstrIncl(this string text, int from, int to)
        {
            return text.Substring(from, to - from + 1);
        }

        public static string SubstrExcl(this string text, int from, int to)
        {
            return text.Substring(from + 1, to - from - 1);
        }

        public static string GetLast(this string source, int tail_length)
        {
            if (tail_length >= source.Length) return source;
            return source.Substring(source.Length - tail_length);
        }

        public static string GetFirst(this string source, int length)
        {
            return source?.Substring(0, Math.Min(source.Length, length));
        }

        #endregion


        #region DateTime Extension

        public static DateTime Round(this DateTime dt, TimeSpan d, string round = "R")
        {
            DateTime up = new DateTime(((dt.Ticks + d.Ticks - 1) / d.Ticks) * d.Ticks);
            DateTime down = up.Subtract(d);
            if (round == "U") return up;
            if (round == "D") return down;
            if (round == "R")
            {
                double d1 = dt.Subtract(down).TotalMilliseconds;
                double d2 = up.Subtract(dt).TotalMilliseconds;
                if (d1 > d2) return up; else return down;
            }
            return dt;
        }

        public static int ToUnixTimestamp(this DateTime? dateTime)
        {
            DateTime datum = dateTime ?? new DateTime(1971, 1, 1, 0, 0, 0);
            return Convert.ToInt32((TimeZoneInfo.ConvertTimeToUtc(datum) - new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds);
        }

        public static DateTime forXML(this DateTime dateTime)
        {
            var dt = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local);
            return dt.AddTicks(-(dt.Ticks % TimeSpan.TicksPerSecond));            
        }

        public static string forSQL(this DateTime dt)
        {
            return dt.Year.ToString() + "-" + dt.Month.ToString("00") + "-" + dt.Day.ToString("00") + "T" + dt.Hour.ToString("00") + ":" + dt.Minute.ToString("00") + ":" + dt.Second.ToString("00");
        }

        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            dt = dt.Date;
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static DateTime EndOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            var end = dt.StartOfWeek(startOfWeek);
            return end.AddDays(7).AddSeconds(-1);
        }

        #endregion


        #region Decimal Extensions

        public static decimal Round4(this decimal num)
        {
            return decimal.Parse(Math.Round(num, 4).ToString("0.0000"));
        }

        public static decimal Round4(this decimal? num)
        {
            return decimal.Parse(Math.Round((num ?? 0m), 4).ToString("0.0000"));
        }

        public static decimal Round6(this decimal num)
        {
            return decimal.Parse(num.ToString("0.000000"));
        }

        public static decimal Round6(this decimal? num)
        {
            return decimal.Parse((num ?? 0m).ToString("0.000000"));
        }

        public static decimal Round2(this decimal num)
        {
            return decimal.Parse(Math.Round(num, 2).ToString("0.00"));
        }

        public static decimal Round2(this decimal? num)
        {
            return decimal.Parse(Math.Round((num ?? 0m), 2).ToString("0.00"));
        }

        #endregion


        public static string ToXML(this object type)
        {
            using (var stringwriter = new System.IO.StringWriter())
            {
                var ns = new XmlSerializerNamespaces();                
                ns.Add("", "https://efi.tax.gov.me/fs/schema");                

                var serializer = new XmlSerializer(type.GetType());
                var settings = new XmlWriterSettings();
                settings.Indent = false;
                settings.CheckCharacters = true;
                settings.ConformanceLevel = ConformanceLevel.Document;
                settings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
                settings.NewLineOnAttributes = false;
                settings.OmitXmlDeclaration = true;

                using (var stream = new StringWriter())
                using (var writer = XmlWriter.Create(stream, settings))
                {
                    serializer.Serialize(writer, type, ns);
                    return stream.ToString();
                }
            }
        }


        public static async Task<byte[]> GetBytes(this IFormFile formFile)
        {
            await using var memoryStream = new MemoryStream();
            await formFile.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        
        public static string? BaseUrl(this HttpRequest req)
        {
            if (req == null) return null;
            var uriBuilder = new UriBuilder(req.Scheme, req.Host.Host, req.Host.Port ?? -1);
            if (uriBuilder.Uri.IsDefaultPort)
            {
                uriBuilder.Port = -1;
            }

            return uriBuilder.Uri.AbsoluteUri;
        }
        
    }
}
