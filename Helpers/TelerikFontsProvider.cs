using System.Runtime.CompilerServices;
using Telerik.Documents.Core.Fonts;
using Telerik.Windows.Documents.Core.Fonts;

namespace Oblak.Helpers
{
    public class TelerikFontsProvider : Telerik.Windows.Documents.Extensibility.FontsProviderBase
    {
        private readonly string fontFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

        public override byte[] GetFontData(FontProperties fontProperties)
        {
            string fontFamilyName = fontProperties.FontFamilyName;
            bool isBold = fontProperties.FontWeight == FontWeights.Bold;
            string fontFileName = fontProperties.FontFamilyName + ".ttf";
            string fontFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string targetPath = Path.Combine(fontFolder, fontFileName);

            if (fontFamilyName == "Segoe UI Semibold")
            {
                return this.GetFontDataFromFontFolder("seguisb.ttf");
            }
            else if (fontFamilyName == "Arial")
            {
                return this.GetFontDataFromFontFolder("arial.ttf");
            }
            else if (fontFamilyName == "Calibri" && isBold)
            {
                return this.GetFontDataFromFontFolder("calibrib.ttf");
            }
            else if (fontFamilyName == "Calibri")
            {
                return this.GetFontDataFromFontFolder("calibri.ttf");
            }

            DirectoryInfo directory = new DirectoryInfo(fontFolder);
            FileInfo[] fontFiles = directory.GetFiles("*.ttf");
            if (fontFiles.Any(s => s.Name.Equals(fontFileName, StringComparison.InvariantCultureIgnoreCase)))
            {
                using (FileStream fileStream = File.OpenRead(targetPath))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        fileStream.CopyTo(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            }

            return null;
        }
        private byte[] GetFontDataFromFontFolder(string fontFileName)
        {
            using (FileStream fileStream = File.OpenRead(this.fontFolder + "\\" + fontFileName))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    fileStream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }
    }    
}
