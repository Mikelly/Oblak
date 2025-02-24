using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Oblak.Data;
using Oblak.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using Oblak.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Oblak.Controllers
{
	[Authorize]		
	public class MneMupController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly ApplicationUser _appUser;
		private readonly int _legalEntityId;
		private readonly LegalEntity _legalEntity;

		public MneMupController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
		{
			_db = context;

			var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
			_appUser = _db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
			_legalEntityId = _appUser.LegalEntityId;
			_legalEntity = _appUser.LegalEntity;
		}

		[HttpGet]
        [Route("/mnemup")]
        public IActionResult Upload()
		{
			return View();
		}

		[HttpPost]
		[Route("upload-data")]
		public async Task<IActionResult> UploadExcel(IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest("No file uploaded");

			var records = new List<MneMupData>();

			using (var stream = new MemoryStream())
			{
				await file.CopyToAsync(stream);
				using (var package = new ExcelPackage(stream))
				{
					ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
					if (worksheet == null)
						return BadRequest("No worksheet found");

					int rowCount = worksheet.Dimension.Rows;
					for (int row = 2; row <= rowCount; row++) // Assuming row 1 is the header
					{
						try
						{
							var entity = new MneMupData
							{

								LegalEntityName = worksheet.Cells[row, 1].Text, // DAVALAC
								Address = worksheet.Cells[row, 2].Text, // ADRESA_OBJEKTA
								TIN = worksheet.Cells[row, 3].Text, // PIB_MB
								DateOfBirth = DateTime.ParseExact(worksheet.Cells[row, 4].Text, "dd.MM.yyyy", CultureInfo.InvariantCulture), // DATUMRODJENJA
								DocumentCountry = worksheet.Cells[row, 5].Text, // DOKUMENT_IZDALA
								CheckIn = DateTime.ParseExact(worksheet.Cells[row, 6].Text, "dd.MM.yyyy", CultureInfo.InvariantCulture), // DATUMPRIJAVE
								CheckOut = DateTime.ParseExact(worksheet.Cells[row, 7].Text == "" ? worksheet.Cells[row, 6].Text : worksheet.Cells[row, 7].Text, "dd.MM.yyyy", CultureInfo.InvariantCulture), // DATUMODJAVE
								Gender = worksheet.Cells[row, 8].Text, // POL
								PartnerId = _legalEntity.Id,
								LegalEntityCode = worksheet.Cells[row, 9].Text // SIFRA_DAVALAC
							};
							records.Add(entity);
						}
						catch (Exception ex)
						{
							return BadRequest($"Error processing row {row}: {ex.Message}");
						}
					}
				}
			}

			_db.MneMupData.AddRange(records);
			await _db.SaveChangesAsync();

			return Ok(new { message = "File processed successfully", recordsAdded = records.Count });
		}
	}
}
