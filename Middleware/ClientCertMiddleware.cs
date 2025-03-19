using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oblak.Data;

namespace Oblak.Middleware;

public class ClientCertMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<ClientCertMiddleware> _logger;
	private readonly PathString _loginPath;

	public ClientCertMiddleware(RequestDelegate next, ILogger<ClientCertMiddleware> logger, string loginPath = "/sign-in")
	{
		_next = next;
		_logger = logger;
		_loginPath = new PathString(loginPath);
	}

	public async Task Invoke(HttpContext context)
	{
		// Check if the user is already logged in (authenticated)
		if (context.User?.Identity?.IsAuthenticated == true)
		{
			_logger.LogInformation("User is already authenticated: {UserName}", context.User.Identity.Name);
			await _next(context);
			return;
		}

		// Skip processing for the login page to avoid redirect loops.
		if (context.Request.Path.StartsWithSegments(_loginPath))
		{
			await _next(context);
			return;
		}

		// Only process HTTPS requests.
		if (!context.Request.IsHttps)
		{
			await _next(context);
			return;
		}

		// Attempt to retrieve the client certificate.
		X509Certificate2 clientCert = await context.Connection.GetClientCertificateAsync();
		if (clientCert != null)
		{
            string subject = clientCert.Subject;
			string cn = ExtractCommonName(subject);

			context.Items["ClientCertCN"] = cn;

			if (!string.IsNullOrEmpty(cn))
			{
				_logger.LogInformation("Client certificate detected with CN: {CommonName}", cn);

				// Retrieve the ApplicationDbContext from DI.
				var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();

				// Example: Check if the Users table contains a record where ClientCertCN equals the extracted CN.
				bool isValidUser = await dbContext.Users.AnyAsync(u => u.UserName == cn);
				if (isValidUser)
				{
					// Redirect to login page with the CN as a query parameter.
					string redirectUrl = $"{_loginPath}?username=" + WebUtility.UrlEncode(cn);
					context.Response.Redirect(redirectUrl);
					return;
				}
				else
				{
					_logger.LogInformation("CN {CommonName} is not configured for client certificate login in the database.", cn);
				}
			}
			else
			{
				_logger.LogWarning("Client certificate provided, but could not extract CN from subject: {Subject}", subject);
			}
		}
		else
		{
			_logger.LogInformation("No client certificate detected.");
		}

		// Proceed to the next middleware.
		await _next(context);
	}

	// Helper method to extract the Common Name (CN) from the certificate's subject string.
	private string ExtractCommonName(string subject)
	{
		const string cnPrefix = "CN=";
		int startIndex = subject.IndexOf(cnPrefix, StringComparison.OrdinalIgnoreCase);
		if (startIndex >= 0)
		{
			startIndex += cnPrefix.Length;
			int endIndex = subject.IndexOf(',', startIndex);
			if (endIndex < 0)
			{
				endIndex = subject.Length;
			}
			return subject.Substring(startIndex, endIndex - startIndex).Trim();
		}
		return string.Empty;
	}
}
