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

        public byte[] GenerateErrorPdf(string message)
        {
            var report = new Telerik.Reporting.Report();
            var detail = new Telerik.Reporting.DetailSection
            {
                Height = Telerik.Reporting.Drawing.Unit.Cm(2)
            };
            var textBox = new Telerik.Reporting.TextBox
            {
                Value = message,
                Size = new Telerik.Reporting.Drawing.SizeU(
                    Telerik.Reporting.Drawing.Unit.Cm(15),
                    Telerik.Reporting.Drawing.Unit.Cm(2)),
                Style =
                {
                    Color = System.Drawing.Color.Red
                }
            };
            detail.Items.Add(textBox);
            report.Items.Add(detail);

            var processor = new Telerik.Reporting.Processing.ReportProcessor();
            var result = processor.RenderReport("PDF", new Telerik.Reporting.InstanceReportSource
            {
                ReportDocument = report
            }, null);

            return result.DocumentBytes;
        }
    }
}
