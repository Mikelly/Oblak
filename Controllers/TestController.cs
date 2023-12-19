using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Schedulers;
using Oblak.Interfaces;
using Oblak.Models;
using Oblak.Models.Srb;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;
using System.Diagnostics;

namespace Oblak.Controllers
{
    [Route("Test")]
    public class TestController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MneClient _client;
        private readonly SrbClient _srbClient;
        private readonly SrbScheduler _srbScheduler;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Register _registerClient;
        private ApplicationUser _appUser;

        public TestController(
            ILogger<HomeController> logger,
            MneClient client,
            SrbClient srbClient,
            ApplicationDbContext db,
            SrbScheduler srbScheduler,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _client = client;
            _srbClient = srbClient;
            _db = db;
            _mapper = mapper;
            _scopeFactory = scopeFactory;
            _srbScheduler = srbScheduler;

            var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (username != null)
            {
                _appUser = db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
                if (_appUser.LegalEntity.Country == Data.Enums.Country.MNE) _registerClient = serviceProvider.GetRequiredService<MneClient>();
                if (_appUser.LegalEntity.Country == Data.Enums.Country.SRB) _registerClient = serviceProvider.GetRequiredService<SrbClient>();
            }
        }

        [HttpPost]
        [Route("testServiceAuth")]
        public async Task<ActionResult> TestServiceAuth()
        {
            await _registerClient.Authenticate();

            return Ok();
        }

        [HttpPost]
        [Route("testServiceAuthLe")]
        public async Task<ActionResult> TestServiceAuth(int legalEntity)
        {
            var le = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);

            await _registerClient.Authenticate(le);

            return Ok();
        }

        [HttpPost]
        [Route("testSrbScheduler")]
        public async Task<ActionResult> TestSrbScheduler()
        {
            await _srbScheduler.DailyCheckOut();

            return Ok();
        }

        [HttpGet]
        public IActionResult CheckRb90(int legalEntity)
        {
            var le = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);
            var result = _client.Auth(le!);
            return Ok(result.Item2);
        }

        [HttpGet]
        [Route("Rb90Test")]
        public async Task<IActionResult> CheckSrb()
        {
            var result1 = await _srbClient.RefreshToken();
            return Ok();
        }

        [HttpPost]
        [Route("SrbTest")]
        public async Task<ActionResult> SrbPersonTest(SrbPersonViewModel person)
        {
            var p = _mapper.Map<SrbPerson>(person);

            var result = await _srbClient.CheckIn(p);

            return Ok(result);
        }

        [HttpPost]
        [Route("SrbGroupTest")]
        public async Task<ActionResult> SrbGroupTest()
        {
            try
            {
                var username = HttpContext.User.Identity.Name;
                var user = _db.Users.Include(a => a.LegalEntity).Where(a => a.UserName == username).FirstOrDefault();
                var property = _db.Properties.Where(a => a.LegalEntityId == user.LegalEntityId).FirstOrDefault();
                var g = new Group();
                g.PropertyId = property.Id;
                g.PropertyExternalId = property.ExternalId;
                g.LegalEntityId = property.LegalEntityId;
                g.Status = "A";
                g.CheckIn = DateTime.Now;
                g.CheckOut = null;
                g.Description = "TEST";
                //g.Email = "";                
                g.Date = DateTime.Now;
                g.Guid = Guid.NewGuid().ToString();
                //g.Phone = "";
                //g.Note = "A";
                _db.Groups.Add(g);
                _db.SaveChanges();
                var p = new SrbPerson();
                p.GroupId = g.Id;
                p.FirstName = "TEST";
                p.LastName = "TEST";
                p.PropertyId = property.Id;
                p.PropertyExternalId = property.ExternalId;
                p.LegalEntityId = property.LegalEntityId;
                p.ExternalId = null;
                p.IsDomestic = false;
                p.IsDeleted = false;
                p.IsForeignBorn = true;                
                p.Gender = "M";
                p.PersonalNumber = "132465798";
                p.BirthDate = DateTime.Now.AddYears(-24);
                p.BirthPlaceName = "Montenegro";
                p.BirthCountryIso2 = "ME";
                p.NationalityIso2 = "ME";
                p.DocumentType = "83";
                p.DocumentNumber = "12345678";
                p.DocumentIssueDate = DateTime.Now.AddYears(-4);
                p.EntryDate = DateTime.Now.Date;
                p.EntryPlaceCode = "1";
                p.EntryPlace = "Aerodrom Beograd";
                p.ArrivalType = "1";
                p.ServiceType = "1";
                p.CheckIn = DateTime.Now.Date;
                p.PlannedCheckOut = DateTime.Now.AddDays(5);
                p.ReasonForStay = "4";
                p.Error = "";
                p.Status = "A";
                _db.SrbPersons.Add(p);
                _db.SaveChanges();

                g = _db.Groups.Where(a => a.Id == g.Id).Include(a => a.Persons).Include(a => a.Property).Include(a => a.LegalEntity).FirstOrDefault();

                var result = await _srbClient.RegisterGroup(g, DateTime.Now, null);
                //}
            }
            catch (Exception e)
            {
                foreach (var entityEntry in _db.ChangeTracker.Entries().Where(et => et.State != EntityState.Unchanged))
                {
                    foreach (var entry in entityEntry.CurrentValues.Properties)
                    {
                        var prop = entityEntry.Property(entry.Name).Metadata;
                        var value = entry.PropertyInfo?.GetValue(entityEntry.Entity);
                        var valueLength = value?.ToString()?.Length;
                        var typemapping = prop.GetTypeMapping();
                        var typeSize = ((Microsoft.EntityFrameworkCore.Storage.RelationalTypeMapping)typemapping).Size;
                        if (typeSize.HasValue && valueLength > typeSize.Value)
                        {
                            _logger.LogError($"Truncation will occur: {entityEntry.Metadata.GetTableName()}.{prop.GetColumnName()} {prop.GetColumnType()} :: {entry.Name}({valueLength}) = {value}");
                        }
                    }
                }
            }
            return Ok();
        }
    }
}