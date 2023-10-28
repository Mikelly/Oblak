using System.IO.Pipelines;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf.Streaming;

namespace Oblak.Helpers
{
    public class Pdf
    {
        public async Task<Stream> Merge(List<Stream> streams)
        {
            var main_path = Guid.NewGuid().ToString() + ".pdf";
            var memStream = new MemoryStream();
            using (PdfStreamWriter fileWriter = new PdfStreamWriter(File.OpenWrite(main_path)))
            {
                foreach (var stream in streams)
                {
                    var path = Guid.NewGuid().ToString() + ".pdf";
                    var fileStream = File.Create(path);
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                    fileStream.Close();

                    using (PdfFileSource fileToMerge = new PdfFileSource(File.OpenRead(path)))
                    {
                        foreach (PdfPageSource pageToMerge in fileToMerge.Pages)
                        {
                            fileWriter.WritePage(pageToMerge);
                        }
                    }

                    File.Delete(path);
                }
            }
            return File.OpenRead(main_path);
        }
    }
}
