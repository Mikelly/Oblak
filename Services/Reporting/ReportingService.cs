using Oblak.Data;
using Telerik.Reporting.Processing;
using Telerik.Reporting;
using Oblak.Controllers;

namespace Oblak.Services.Reporting
{
    public class ReportingService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ReportingService> _logger;

        public ReportingService( IWebHostEnvironment env, ILogger<ReportingService> logger) { 
            _env = env;
            _logger = logger;
        }

        public byte[] RenderReport(string report, List<Telerik.Reporting.Parameter> parameters, string format) 
        {
            var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            var deviceInfo = new System.Collections.Hashtable();
            var reportSource = new Telerik.Reporting.UriReportSource();

            var reportName = $"{report}.trdp";            
            var path = Path.Combine(_env.ContentRootPath, "Reports", reportName);

            reportSource.Uri = path;

            reportSource.Parameters.AddRange(parameters);
            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport(format, reportSource, deviceInfo);

            return result.DocumentBytes;            
        }
    }
}
