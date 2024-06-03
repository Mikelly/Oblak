using AutoMapper;
using Kendo.Mvc.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.RulesetToEditorconfig;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NuGet.Packaging.Signing;
using NuGet.Protocol.Plugins;
using Oblak.Data;
using Oblak.Helpers;
using Oblak.Interfaces;
using Oblak.Models.Api;
using Oblak.Models.Srb;
using Oblak.Services.Payten;
using Oblak.Services.Reporting;
using Oblak.Services.SRB.Models;
using Oblak.SignalR;
using RB90;
using RestSharp;
using RestSharp.Serializers.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Oblak.Services.SRB;

public class SrbClient : Register
{
    RestClient _client;
    ILogger<SrbClient> _logger;
    IMapper _mapper;
    string _token;
    string _refreshToken;

    public SrbClient(
        ILogger<SrbClient> logger,
        IConfiguration configuration,
        IMapper mapper,
        eMailService eMailService,
        ReportingService reporting,
        SelfRegisterService selfRegisterService,
        IWebHostEnvironment webHostEnvironment,
        IHubContext<MessageHub> messageHub,
        ApplicationDbContext db)
        :
        base(configuration, eMailService, reporting, selfRegisterService, webHostEnvironment, messageHub, db)
    {
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<LoginResponse> Login(LoginRequest loginRequest)
    {
        try
        {
            var loginEndpoint = _configuration["SRB:Endpoints:Login"]!.Trim('/');
            var request = new RestRequest(loginEndpoint, Method.Post).AddJsonBody(loginRequest);
            var response = await _client.ExecutePostAsync(request);
            var jsonstring = JsonSerializer.Deserialize<string>(response.Content!);
            var result = JsonSerializer.Deserialize<LoginResponse>(jsonstring!);
            (_token, _refreshToken) = (result.token!, result.refreshToken!);
            (_legalEntity.SrbRbToken, _legalEntity.SrbRbRefreshToken) = (result.token, result.refreshToken);
            _db.SaveChanges();
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError($"ERROR SRB:Login {e.Message}");
            _logger.LogTrace($"POST {JsonSerializer.Serialize(loginRequest)}");
            throw;
        }
    }


    public async Task<LoginResponse> RefreshToken()
    {
        try
        {
            if (_token != null)
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_token);
                if (jwt.ValidTo < DateTime.Now)
                {
                    await Login(new LoginRequest() { korisnickoIme = _legalEntity.SrbRbUserName, lozinka = _legalEntity.SrbRbPassword });
                }
            }
            else
            {
                await Login(new LoginRequest() { korisnickoIme = _legalEntity.SrbRbUserName, lozinka = _legalEntity.SrbRbPassword });
            }
            var refreshTokenEndpoint = _configuration["SRB:Endpoints:RefreshToken"]!.Trim('/');
            var request = new RestRequest(refreshTokenEndpoint, Method.Get);
            request.AddHeader("Authorization", $"Bearer {_token}");
            request.AddHeader("RefreshToken", $"{_refreshToken}");
            var response = await _client.ExecuteGetAsync(request);
            var jsonstring = JsonSerializer.Deserialize<string>(response.Content!);
            var result = JsonSerializer.Deserialize<LoginResponse>(jsonstring!)!;
            (_token, _refreshToken) = (result.token!, result.refreshToken!);
            (_legalEntity.SrbRbToken, _legalEntity.SrbRbRefreshToken) = (result.token, result.refreshToken);
            _db.SaveChanges();
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError($"ERROR SRB:Refresh token {e.Message}");
            throw;
        }
    }

    public async Task<CheckInOutResponse> CheckIn(SrbPerson person)
    {
        var checkInRequest = Person2CheckInRequest(person);
        try
        {
            var checkInEndpoint = _configuration["SRB:Endpoints:CheckIn"]!.Trim('/');
            var request = new RestRequest(checkInEndpoint, Method.Post);
            request.AddHeader("Authorization", $"Bearer {_token}");
            request.AddHeader("RefreshToken", $"{_refreshToken}");
            var json = JsonSerializer.Serialize(checkInRequest);
            request.AddJsonBody(json);
            //request.AddJsonBody(checkInRequest);
            var response = await _client.ExecutePostAsync(request);
            //var jsonstring = JsonSerializer.Deserialize<string>(response.Content!);
            CheckInOutResponse result = JsonSerializer.Deserialize<CheckInOutResponse>(response.Content!)!;
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError($"ERROR SRB:Login {e.Message}");
            _logger.LogTrace($"POST CHECK IN {JsonSerializer.Serialize(checkInRequest)}");
            throw;
        }
    }

    public async Task<CheckInOutResponse> CheckOut(SrbPerson person)
    {
        var checkOutRequest = Person2CheckOutRequest(person);
        try
        {
            var checkOutEndpoint = _configuration["SRB:Endpoints:CheckOut"]!.Trim('/');
            var request = new RestRequest(checkOutEndpoint, Method.Post);
            request.AddHeader("Authorization", $"Bearer {_token}");
            request.AddHeader("RefreshToken", $"{_refreshToken}");
            var json = JsonSerializer.Serialize(checkOutRequest);
            request.AddJsonBody(json);
            var response = await _client.ExecutePostAsync(request);
            //var jsonstring = JsonSerializer.Deserialize<string>(response.Content!);
            var result = JsonSerializer.Deserialize<CheckInOutResponse>(response.Content!)!;
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError($"ERROR SRB:Login {e.Message}");
            _logger.LogTrace($"POST CHECK OUT {JsonSerializer.Serialize(checkOutRequest)}");
            throw;
        }
    }


    public override async Task<List<PersonErrorDto>> RegisterGroup(Group group, DateTime? checkInDate, DateTime? checkOutDate)
    {
        try
        {
            await Authenticate(group.Property.LegalEntity);

            var test = _legalEntity.Test;
            var data = _db.SrbPersons.Include(a => a.Property).ThenInclude(a => a.LegalEntity).Where(a => a.GroupId == group.Id).ToList();

            if (checkInDate.HasValue)
            {
                foreach (var pr in data) pr.CheckIn = checkInDate.Value;
            }

            if (checkOutDate.HasValue)
            {
                foreach (var pr in data) pr.CheckOut = checkOutDate.Value;
            }

            var errors = new List<PersonErrorDto>();

            errors = Validate(group, checkInDate, checkOutDate);

            if (errors.Any()) return errors;

            int total = data.Count();

            try
            {
                var result = await RefreshToken();
                Thread.Sleep(100);
            }
            catch (Exception e)
            {
                _logger.LogError("Register Group Login ERROR - eTurista: " + Exceptions.StringException(e));
                throw new Exception("Greška prilikom autentifikacije na eTourista servis.");
            }

            int c = 0;
            foreach (var pr in data.OrderBy(a => a.ExternalId))
            {
                c++;
                //await _messageHub.Clients.User(_context.User.Identity!.Name!).SendAsync("status",
                //    (int)(Math.Round((decimal)c / (decimal)total * 100m, 0, MidpointRounding.AwayFromZero)),
                //    $"Prijavljivanje gostiju {c}/{total}", $"{pr.FirstName} {pr.LastName}"
                //    );

                try
                {
                    if (checkOutDate.HasValue)
                    {
                        var response = await CheckOut(pr);
                        if (response.errors.Any()) pr.Error = JsonSerializer.Serialize(response.errors);
                        else { pr.CheckedOut = true; pr.ExternalId = int.Parse(response.identifikator); pr.Error = null; }
                    }
                    else
                    {
                        var response = await CheckIn(pr);
                        if (response.errors.Any()) pr.Error = JsonSerializer.Serialize(response.errors);
                        else { pr.CheckedIn = true; pr.ExternalId = int.Parse(response.identifikator); pr.Error = null; }
                        _db.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    pr.Error = Exceptions.StringException(e);
                    _db.SaveChanges();
                }
            }

            await GetExternalIds(group);

            //await _messageHub.Clients.User(_context.User.Identity!.Name!).SendAsync("status", 100, $"Prijavljivanje završeno");

            if (data.Any(a => a.Error != null))
            {
                foreach (var err in data.Where(a => a.Error != null))
                {
                    errors.Add(new PersonErrorDto() { PersonId = $"{err.FirstName} {err.LastName}", ExternalErrors = err.Error != null ? JsonSerializer.Deserialize<List<string>>(err.Error)! : null });
                }
            }

            if (errors.Any()) return errors;
            else return null;
        }
        catch (Exception e)
        {
            throw;
        }
    }


    public override async Task<PersonErrorDto> RegisterPerson(Person person, DateTime? checkInDate, DateTime? checkOutDate)
    {
		try
		{
			var test = _legalEntity.Test;

            var pr = person as SrbPerson;

			if (checkInDate.HasValue)
			{
				pr.CheckIn = checkInDate.Value;
			}

			if (checkOutDate.HasValue)
			{
				pr.CheckOut = checkOutDate.Value;
			}

			var error = Validate(person, checkInDate, checkOutDate);
			if (error.ValidationErrors.Any()) return error;

			try
			{
				var result = await RefreshToken();
				Thread.Sleep(100);
			}
			catch (Exception e)
			{
				_logger.LogError("Register Group Login ERROR - eTurista: " + Exceptions.StringException(e));
				throw new Exception("Greška prilikom autentifikacije na eTourista servis.");
			}

            try
            {
                if (checkOutDate.HasValue)
                {
                    var response = await CheckOut(pr);
                    if (response.errors.Any()) pr.Error = JsonSerializer.Serialize(response.errors);
                    else { pr.CheckedOut = true; pr.ExternalId = int.Parse(response.identifikator); pr.Error = null; }
                }
                else
                {
                    var response = await CheckIn(pr);
                    if (response.errors.Any()) pr.Error = JsonSerializer.Serialize(response.errors);
                    else { pr.CheckedIn = true; pr.ExternalId = int.Parse(response.identifikator); pr.Error = null; }
                    _db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                pr.Error = Exceptions.StringException(e);
                _db.SaveChanges();
            }			

			await GetExternalId(pr);

            if (pr.Error != null) error.ExternalErrors = JsonSerializer.Deserialize<List<string>>(pr.Error)!;

			if (error.ExternalErrors.Any()) return error;
			else return null;
		}
		catch (Exception e)
		{
			throw;
		}
	}


    public CheckInRequest Person2CheckInRequest(SrbPerson person)
    {
        CheckInRequest request = new CheckInRequest()
        {
            OsnovniPodaci = new osnovniPodaci
            {
                Izmena = person.CheckedIn ? true : false,
                ExternalId = person.Guid,
                Ime = person.FirstName,
                Prezime = person.LastName,
                Jmbg = person.IsDomestic ? person.PersonalNumber : null,
                PolSifra = person.Gender,
                DatumRodjenja = person.BirthDate.ToString("yyyy-MM-dd"),
                MestoRodjenjaNaziv = person.BirthPlaceName,
                DaLiJeLiceDomace = person.IsDomestic,
                DaLiJeLiceRodjenoUInostranstvu = person.IsForeignBorn,
                DrzavljanstvoAlfa2 = person.NationalityIso2,
                DrzavljanstvoAlfa3 = person.NationalityIso3,
                DrzavaRodjenjaAlfa2 = person.BirthCountryIso2,
                DrzavaRodjenjaAlfa3 = person.BirthCountryIso3,
                //DrzavaPrebivalistaAlfa2 = person.ResidenceCountryIso2,
                //DrzavaPrebivalistaAlfa3 = person.ResidenceCountryIso3,
                //MestoPrebivalistaMaticniBroj = person.ResidencePlaceCode,
                //MestoPrebivalistaNaziv = person.ResidencePlaceName,
                //OpstinaPrebivalistaMaticniBroj = person.ResidenceMunicipalityCode,
                //OpstinaPrebivalistaNaziv = person.ResidenceMunicipalityName,
            },
            PodaciOBoravku = new podaciOBoravku
            {
                UgostiteljskiObjekatJedinstveniIdentifikator = person.Property.ExternalId.ToString(),
                UslovZaUmanjenjeBoravisneTakseSifra = person.ResidenceTaxDiscountReason,
                NacinDolaskaSifra = person.ArrivalType,
                VrstaPruzenihUslugaSifra = person.ServiceType,
                DatumICasDolaska = person.CheckIn.Value.ToString("yyyy-MM-dd HH:mm"),
                PlaniraniDatumOdlaska = person.PlannedCheckOut.Value.ToString("yyyy-MM-dd"),
                RazlogBoravkaSifra = person.ReasonForStay,
                SmestajneJedinice = null,
                NazivAgencije = null,
                BarkodoviVaucera = null,
            },
            IdentifikacioniDokumentStranogLica = new identifikacioniDokumentStranogLica
            {
                BrojPutneIsprave = person.DocumentNumber,
                VrstaPutneIspraveSifra = person.DocumentType,

                MestoUlaskaURepublikuSrbiju = null,
                MestoUlaskaURepublikuSrbijuSifra = null,
                DatumUlaskaURepublikuSrbiju = null,
                DatumDoKadaJeOdobrenBoravakURepubliciSrbiji = null,

                //MestoUlaskaURepublikuSrbiju = person.EntryPlace,
                //MestoUlaskaURepublikuSrbijuSifra = person.EntryPlaceCode,
                //DatumUlaskaURepublikuSrbiju = person.EntryDate.HasValue ? person.EntryDate.Value.ToString("yyyy-MM-dd") : null,

                DatumIzdavanjaPutneIsprave = person.DocumentIssueDate.HasValue ? person.DocumentIssueDate.Value.ToString("yyyy-MM-dd") : null,
                OrganIzdavanjaPutneIsprave = person.IssuingAuthorithy,
                VrstaVizeSifra = person.VisaType,
                BrojVize = person.VisaNumber,
                MestoIzdavanjaVize = person.VisaIssuingPlace,
                Napomena = person.Note,
            }
        };

        if (person.IsDomestic == true)
        {
            request.OsnovniPodaci.DrzavaPrebivalistaAlfa2 = person.ResidenceCountryIso2;
            request.OsnovniPodaci.DrzavaPrebivalistaAlfa3 = person.ResidenceCountryIso3;
            request.OsnovniPodaci.MestoPrebivalistaMaticniBroj = person.ResidencePlaceCode;
            request.OsnovniPodaci.MestoPrebivalistaNaziv = person.ResidencePlaceName;
            request.OsnovniPodaci.OpstinaPrebivalistaMaticniBroj = person.ResidenceMunicipalityCode;
            request.OsnovniPodaci.OpstinaPrebivalistaNaziv = person.ResidenceMunicipalityName;
        }

        if (person.IsDomestic == false)
        {
            request.IdentifikacioniDokumentStranogLica.MestoUlaskaURepublikuSrbiju = person.EntryPlace;
            request.IdentifikacioniDokumentStranogLica.MestoUlaskaURepublikuSrbijuSifra = person.EntryPlaceCode;
            request.IdentifikacioniDokumentStranogLica.DatumUlaskaURepublikuSrbiju = person.EntryDate.HasValue ? person.EntryDate.Value.ToString("yyyy-MM-dd") : null;
        }

        if (new string[] { "72", "85", "86", "87", "88" }.Contains(person.DocumentType))
        {
            request.IdentifikacioniDokumentStranogLica.DatumDoKadaJeOdobrenBoravakURepubliciSrbiji = person.StayValidTo.HasValue ? person.StayValidTo.Value.ToString("yyyy-MM-dd") : null;
        }

        if (person.ExternalId.HasValue && person.ExternalId.Value > 0)
        {
            request.OsnovniPodaci.Izmena = true;
        }

        if (person.Property.Type != "10" && person.Property.Type != "12")
        {
            request.PodaciOBoravku.SmestajneJedinice = new SmestajnaJedinica[] 
            {
                new SmestajnaJedinica()
                { 
                    BrojSmestajneJedinice = "1",
                    SpratSmestajneJedinice = "1",
                    JedinstveniIdentifikator = 0,
                    JeObrisan = 0,
                    //DatumBoravkaDo = person.PlannedCheckOut.Value.ToString("yyyy-MM-dd HH:mm"),
                    DatumBoravkaOd = person.CheckIn.Value.ToString("yyyy-MM-dd HH:mm")
                }
            };
        }

        return request;
    }


    public CheckOutRequest Person2CheckOutRequest(SrbPerson person)
    {
        CheckOutRequest request = new CheckOutRequest()
        {
            Izmena = person.CheckedOut ? true : false,
            DatumICasOdjave = (person.PlannedCheckOut ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm"),
            ExternalId = person.Guid,
            UgostiteljskiObjekatJedinstveniIdentifikator = person.Property.ExternalId,
            BrojPruzenihUslugaSmestaja = null
        };

        if (person.Property.LegalEntity.Type == Data.Enums.LegalEntityType.Company) 
        {
            request.BrojPruzenihUslugaSmestaja = person.NumberOfServices ?? 1;
        }

        return request;
    }

    public override Task<List<CodeList>> CodeLists()
    {
        return _db.CodeLists.Where(a => a.Country == Data.Enums.Country.SRB.ToString()).ToListAsync();
    }

    public void SetUp(LegalEntity legalEntity)
    {
        var test = legalEntity.Test;
        var url = _configuration[$"SRB:{(test ? "TEST" : "PROD")}:URL"]!;
        var options = new RestClientOptions(url);
        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new CustomDateTimeConverter("yyyy-MM-ddThh:mm:ss.000Z"));
        _client = new RestClient(options, configureSerialization: s => s.UseSystemTextJson(jsonOptions));
        _token = legalEntity.SrbRbToken!;
        _refreshToken = legalEntity.SrbRbRefreshToken!;
    }

    public override async Task<object> Authenticate(LegalEntity? legalEntity = null)
    {
        if (legalEntity != null)
        {
            _legalEntity = legalEntity;
        }
        
        SetUp(_legalEntity);
        
        try
        {
            if (_token != null)
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_token);
                //if (jwt.ValidTo < DateTime.Now)
                {
                    await Login(new LoginRequest() { korisnickoIme = _legalEntity.SrbRbUserName!, lozinka = _legalEntity.SrbRbPassword! });
                }
            }
            else
            {
                await Login(new LoginRequest() { korisnickoIme = _legalEntity.SrbRbUserName!, lozinka = _legalEntity.SrbRbPassword! });
            }
            var refreshTokenEndpoint = _configuration["SRB:Endpoints:RefreshToken"]!.Trim('/');
            var request = new RestRequest(refreshTokenEndpoint, Method.Get);
            request.AddHeader("Authorization", $"Bearer {_token}");
            request.AddHeader("RefreshToken", $"{_refreshToken}");
            var response = await _client.ExecuteGetAsync(request);
            var jsonstring = JsonSerializer.Deserialize<string>(response.Content!);
            var result = JsonSerializer.Deserialize<LoginResponse>(jsonstring!)!;
            (_token, _refreshToken) = (result.token!, result.refreshToken!);
            (_legalEntity.SrbRbToken, _legalEntity.SrbRbRefreshToken) = (result.token, result.refreshToken);
            _db.SaveChanges();
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError($"ERROR SRB:Refresh token {e.Message}");
            throw;
        }
    }

	public override async Task ConfirmationGroupMail(Group group, string email)
	{
		var template = _configuration["REPORTING:SRB:ConfirmationEmailTemplate"];
		var senderEmail = _configuration["SendGrid:EmailAddress"];
		var pdfStream = await ConfirmationGroupPdf(group);
		await _eMailService.SendMail(senderEmail, email ?? group.Email, template, new
		{
			subject = $@"donotreply: Potvrde o prijavi boravka",
			message = $"U prilogu se nalaze potvrde o prijavi boravka",
			sender = group.Property.LegalEntity.Name
		}, ("Potvrde.pdf", pdfStream));
	}


	public override async Task ConfirmationPersonMail(Person person, string email)
	{
		var template = _configuration["REPORTING:SRB:ConfirmationEmailTemplate"];
		var senderEmail = _configuration["SendGrid:EmailAddress"];
		var pdfStream = await ConfirmationPersonPdf(person);
		await _eMailService.SendMail(senderEmail, email, template, new
		{
			subject = $@"donotreply: Potvrda o prijavi boravka",
			message = $"U prilogu se nalazi potvrda o prijavi boravka",
			sender = person.Property.LegalEntity.Name
		}, ("Potvrda.pdf", pdfStream));
	}

	public override async Task<Stream> ConfirmationGroupPdf(Group group)
    {
        if (_client == null) Authenticate(group.Property.LegalEntity);
        var ids = await GetExternalIds(group);
        var streams = new List<Stream>();
        foreach (int id in ids)
        {
            try
            {
                var confirmationPdfEndpoint = _configuration["SRB:Endpoints:Confirmation"]!.Trim('/');
                var request = new RestRequest(confirmationPdfEndpoint, Method.Post);
                request.AddHeader("Authorization", $"Bearer {_token}");
                request.AddHeader("RefreshToken", $"{_refreshToken}");
                var json = JsonSerializer.Serialize(id);
                request.AddJsonBody(json);
                var response = await _client.ExecutePostAsync(request);
                var result = new MemoryStream(response.RawBytes!);
                streams.Add(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"ERROR SRB: Confirmation {e.Message}");
                _logger.LogTrace($"POST Confirmation {JsonSerializer.Serialize(85284)}");
                throw;
            }
        }
        return await new Pdf().Merge(streams);
    }

    public override async Task<Stream> ConfirmationPersonPdf(Person person)
    {
        var ids = new int[] { (person as SrbPerson).ExternalId2 ?? 0 };
        var streams = new List<Stream>();
        foreach (int id in ids)
        {
            try
            {
                var confirmationPdfEndpoint = _configuration["SRB:Endpoints:Confirmation"]!.Trim('/');
                var request = new RestRequest(confirmationPdfEndpoint, Method.Post);
                request.AddHeader("Authorization", $"Bearer {_token}");
                request.AddHeader("RefreshToken", $"{_refreshToken}");
                var json = JsonSerializer.Serialize(id);
                request.AddJsonBody(json);
                var response = await _client.ExecutePostAsync(request);
                var result = new MemoryStream(response.RawBytes!);
                streams.Add(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"ERROR SRB: Confirmation {e.Message}");
                _logger.LogTrace($"POST Confirmation {JsonSerializer.Serialize(85284)}");
                throw;
            }
        }
        return await new Pdf().Merge(streams);
    }

    public override async Task<Stream> GuestListPdf(int objekat, string datumod, string datumdo)
    {
        var obj = _db.Properties.FirstOrDefault(a => a.Id == objekat);

        var OD = DateTime.ParseExact(datumod, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
        var DO = DateTime.ParseExact(datumdo, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);

        var result = _reporting.RenderReport("GuestListMne",
            new List<Telerik.Reporting.Parameter>() {
                        new Telerik.Reporting.Parameter("objekat", objekat),
                        new Telerik.Reporting.Parameter("od", OD),
                        new Telerik.Reporting.Parameter("do", DO),
                }
            , "PDF");

        return new MemoryStream(result);
    }


    public override async Task GuestListMail(int objekat, string datumod, string datumdo, string email)
    {
        throw new NotImplementedException();
    }

    public async Task<List<int>> GetExternalIds(Group group)
    {
        throw new NotImplementedException();
    }

    public async Task GetExternalId(SrbPerson p)
    {
		try
		{
			if (p.ExternalId2 == null)			
			{
				var confirmationPdfEndpoint = _configuration["SRB:Endpoints:Persons"]!.Trim('/');
				var request = new RestRequest(confirmationPdfEndpoint, Method.Post);
				request.AddHeader("Authorization", $"Bearer {_token}");
				request.AddHeader("RefreshToken", $"{_refreshToken}");
				var tr = new TuristRequest()
				{
					ime = p.FirstName,
					prezime = p.LastName,
					datumIvremeDolaskaOd = p.CheckIn!.Value.Date.ToString("yyyy-MM-ddTHH:mm:00.0000Z"),
					datumIvremeDolaskaDo = p.CheckIn!.Value.Date.AddDays(1).AddMinutes(-1).ToString("yyyy-MM-ddTHH:mm:00.0000Z"),
					casDolaskaOd = p.CheckIn!.Value.AddMinutes(-1).ToString("HH:mm"),
					casDolaskaDo = p.CheckIn!.Value.AddMinutes(+1).ToString("HH:mm"),
					pageIndex = 0,
					pageSize = 1000
				};
				var json = JsonSerializer.Serialize(tr);
				request.AddJsonBody(json);
				var response = await _client.ExecutePostAsync(request);
				var result = JsonSerializer.Deserialize<TuristResponse>(response.Content!)!;
				if (result.totalRowsCount >= 1)
				{
                    if (result.totalRowsCount == 1)
                    {
                        p.ExternalId2 = result.data.First().turistaId;
                    }
                    else
                    {
                        var results = result.data.Select(a => a.turistaId).ToList();
                        var existing = _db.SrbPersons.Where(a => a.ExternalId2 != null).Where(a => results.Contains(a.ExternalId2.Value)).Select(a => a.ExternalId2.Value).ToList();
                        var hit = results.Except(existing).FirstOrDefault();
                        if(hit != null) p.ExternalId2 = hit;
                    }
					_db.SaveChanges();
				}
			}
		}
		catch (Exception e)
		{
			_logger.LogError($"ERROR SRB: External Ids {e.Message}");
			throw;
		}
	}

    public override async Task SendGuestToken(int propertyId, int? unitId, string email, string phoneNo, string lang)
    {
        throw new NotImplementedException();
    }

    public override async Task<Person> Person(object person)
    {
        SrbPerson srbPerson;
        var dto = person as SrbPersonDto;

        if (dto.Id == 0)
        {
            srbPerson = new SrbPerson();
            dto.Guid = Guid.NewGuid().ToString();
            _db.SrbPersons.Add(srbPerson);
        }
        else
        {
            srbPerson = _db.SrbPersons.FirstOrDefault(a => a.Id == dto.Id)!;
        }

        dto.LegalEntityId = _legalEntity.Id;

        dto.SetEntity(srbPerson);

        //(bool checkedin, bool checkedout) = (srbPerson.CheckedIn, srbPerson.CheckedOut);

        //_mapper.Map(dto, srbPerson);

        //(srbPerson.CheckedIn, srbPerson.CheckedOut) = (checkedin, checkedout);

        _db.SaveChanges();

        return srbPerson;
    }

    public override async Task<Person> PersonFromMrz(MrzDto mrz)
    {
        SrbPerson srbPerson = null;
        return srbPerson;
    }


    public override List<PersonErrorDto> Validate(Group group, DateTime? checkInDate, DateTime? checkOutDate)
    {
        var result = new List<PersonErrorDto>();
        _db.Entry(group).Collection(a => a.Persons).Load();
        foreach (var p in group.Persons)
        {
            var one = Validate(p, checkInDate, checkOutDate);
            if (one.ValidationErrors.Any()) result.Add(one);
        }
        return result;
    }


    public override PersonErrorDto Validate(Person person, DateTime? checkInDate, DateTime? checkOutDate)
    {
        var p = person as SrbPerson;
        var err = new PersonErrorDto();

        if (checkOutDate.HasValue)
        {
            if (p.LegalEntity.Type == Data.Enums.LegalEntityType.Company) if (p.NumberOfServices == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.NumberOfServices), Error = "Podatak 'Broj pruženih usluga' je obavezan za unos." });
            if (p.CheckOut == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.CheckOut), Error = "Morate uneti podatak 'Datum i čas odlaska'." });
            if (p.CheckOut.HasValue && DateTime.Now > p.CheckIn.Value.AddMonths(10)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.CheckIn), Error = "'Datum i čas odlaska' ne sme biti više od 10 meseci u prošlosti." });
        }
        else if (checkInDate.HasValue || p.CheckIn.HasValue)
        {
            if (p.FirstName == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.FirstName), Error = "Podatak 'Ime' je obavezan za unos." });
            if (p.LastName == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.LastName), Error = "Podatak 'Prezime' je obavezan za unos." });
            if (p.BirthDate == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.BirthDate), Error = "Podatak 'Datum rođenja' je obavezan za unos." });
            if (p.Gender == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.Gender), Error = "Podatak 'Pol' je obavezan za unos." });
            // JMBG
            if (p.BirthCountryIso2 == null && p.BirthCountryIso3 == null)
            {
                err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.BirthCountryIso2), Error = "Morate uneti ili podatak 'Država rođenja alfa 2' ili podatak 'Država rođenja alfa 3'." });
                err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.BirthCountryIso3), Error = "Morate uneti ili podatak 'Država rođenja alfa 2' ili podatak 'Država rođenja alfa 3'." });
            }
            if (p.BirthCountryIso2 != null && p.BirthCountryIso3 != null)
            {
                err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.BirthCountryIso2), Error = "Ne smete uneti i podatak 'Država rođenja alfa 2' i podatak 'Država rođenja alfa 3'." });
                err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.BirthCountryIso3), Error = "Ne smete uneti i podatak 'Država rođenja alfa 2' i podatak 'Država rođenja alfa 3'." });
            }
            if (p.PropertyExternalId == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.PropertyExternalId), Error = "Morate uneti podatak 'Jedinstveni identifikator ugostiteljskog objekta'." });
            if (p.ServiceType == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ServiceType), Error = "Morate uneti podatak 'Vrsta pruženih usluga'." });
            if (p.ArrivalType == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ArrivalType), Error = "Morate uneti podatak 'Način dolaska'." });
            if (p.ReasonForStay == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ReasonForStay), Error = "Morate uneti podatak 'Razlog boravka'." });
            if (p.LegalEntity.Type == Data.Enums.LegalEntityType.Person && p.ResidenceTaxDiscountReason != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ResidenceTaxDiscountReason), Error = "Kada je izdavaoc smeštaja fizičko lice, ne treba unositi podatak 'Uslov za umanjenje boravišne takse'." });
            // Naziv agencije
            // Smeštajne jedinice
            if (p.CheckIn == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.CheckIn), Error = "Morate uneti podatak 'Datum i čas dolaska'." });
            if (p.CheckIn.HasValue && DateTime.Now > p.CheckIn.Value.AddHours(26)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.CheckIn), Error = "'Datum i čas dolaska' ne sme biti više od 26 sati u prošlosti." });
            if (p.CheckIn.HasValue && DateTime.Now < p.CheckIn.Value) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.CheckIn), Error = "'Datum i čas dolaska' ne sme biti u budućnosti." });
            if (p.PlannedCheckOut == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.PlannedCheckOut), Error = "Morate uneti podatak 'Planirani datum odlaska'." });
            if (p.PlannedCheckOut.HasValue && p.CheckIn.HasValue && p.PlannedCheckOut < p.CheckIn) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.PlannedCheckOut), Error = "Podatak 'Planirani datum odlaska' ne sme biti pre 'Datuma i časa dolaska'." });

            if (p.IsDomestic)
            {
                if (p.DocumentNumber != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.DocumentNumber), Error = "Kada se prijavljuje domaći gost, ne treba unositi podatak 'Broj putne isprave'." });
                if (p.DocumentType != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.DocumentType), Error = "Kada se prijavljuje domaći gost, ne treba unositi podatak 'Vrsta putne isprave'." });
                if (p.DocumentType != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.DocumentIssueDate), Error = "Kada se prijavljuje domaći gost, ne treba unositi podatak 'Datum izdavanja putne isprave'." });
                if (p.IssuingAuthorithy != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.IssuingAuthorithy), Error = "Kada se prijavljuje domaći gost, ne treba unositi podatak 'Organ izdavanja putne isprave'." });

                if (p.EntryPlaceCode != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.EntryPlaceCode), Error = "Kada se prijavljuje domaći gost, ne treba unositi podatak 'Šifra mesta ulaska u republiku Srbiju'." });
                if (p.EntryPlace != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.EntryPlace), Error = "Kada se prijavljuje domaći gost, ne treba unositi podatak 'Mesto ulaska u republiku Srbiju'." });
                if (p.StayValidTo != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.StayValidTo), Error = "Kada se prijavljuje domaći gost, ne treba unositi podatak 'Datum do kada je odobren boravak u republici Srbiji'." });

                if (p.VisaType != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.VisaType), Error = "Kada se prijavljuje domaći gost, ne treba unositi podatak 'Vrsta vize'." });
                if (p.VisaNumber != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.VisaNumber), Error = "Kada se prijavljuje domaći gost, ne treba unositi podatak 'Broj vize'." });
                if (p.VisaIssuingPlace != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.VisaIssuingPlace), Error = "Kada se prijavljuje domaći gost, ne treba unositi podatak 'Mesto izdavanja vize'." });

                if (p.Note != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.Note), Error = "Kada se prijavljuje domaći gost, ne treba unositi podatak 'Napomena'." });

                if (p.ResidenceCountryIso2 == null && p.ResidenceCountryIso3 == null)
                {
                    err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ResidenceCountryIso2), Error = "Morate uneti ili podatak 'Država prebivališta alfa 2' ili podatak 'Država prebivališta alfa 3'." });
                    err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ResidenceCountryIso3), Error = "Morate uneti ili podatak 'Država prebivališta alfa 2' ili podatak 'Država prebivališta alfa 3'." });
                }
                if (p.ResidenceCountryIso2 != null && p.ResidenceCountryIso3 != null)
                {
                    err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ResidenceCountryIso2), Error = "Ne smete uneti i podatak 'Država prebivališta alfa 2' i podatak 'Država prebivališta alfa 3'." });
                    err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ResidenceCountryIso3), Error = "Ne smete uneti i podatak 'Država prebivališta alfa 2' i podatak 'Država prebivališta alfa 3'." });
                }
                if (p.ResidenceCountryIso2 == "RS" || p.ResidenceCountryIso3 == "SRB")
                {
                    if (p.ResidenceMunicipalityCode == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ResidenceMunicipalityCode), Error = "Morate uneti podatak 'Matični broj opštine previbališta'." });
                    if (p.ResidenceMunicipalityName == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ResidenceMunicipalityName), Error = "Morate uneti podatak 'Naziv opštine previbališta'." });
                    if (p.ResidencePlaceCode == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ResidencePlaceCode), Error = "Morate uneti podatak 'Matični broj mesta previbališta'." });
                }

                if (p.ResidencePlaceName == null || p.ResidencePlaceName.StartsWith(" ")) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ResidencePlaceName), Error = "Morate uneti podatak 'Naziv mesta previbališta' i naziv ne sme počinjati sa razmakom." });
            }
            else
            {
                //if (p.ExternalId.HasValue) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ExternalId), Error = "Podaci o stranom državljaninu se ne mogu menjati." });

                if (p.NationalityIso2 == null && p.NationalityIso3 == null)
                {
                    err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.NationalityIso2), Error = "Morate uneti ili podatak 'Nacionalnost alfa 2' ili podatak 'Nacionalnost alfa 3'." });
                    err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.NationalityIso3), Error = "Morate uneti ili podatak 'Nacionalnost alfa 2' ili podatak 'Nacionalnost alfa 3'." });
                }
                if (p.NationalityIso2 != null && p.NationalityIso3 != null)
                {
                    err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.NationalityIso2), Error = "Ne smete uneti i podatak 'Nacionalnost alfa 2' i podatak 'Nacionalnost alfa 3'." });
                    err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.NationalityIso3), Error = "Ne smete uneti i podatak 'Nacionalnost alfa 2' i podatak 'Nacionalnost alfa 3'." });
                }

                if (p.DocumentNumber == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.DocumentNumber), Error = "Kada se prijavljuje strani gost, morate uneti podatak 'Broj putne isprave'." });
                if (p.DocumentType == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.DocumentType), Error = "Kada se prijavljuje strani gost, morate uneti podatak 'Vrsta putne isprave'." });
                if (p.DocumentType == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.DocumentIssueDate), Error = "Kada se prijavljuje strani gost, morate uneti podatak 'Datum izdavanja putne isprave'." });
                //if (p.IssuingAuthorithy != null) err.Errors.Add(new PersonFieldError() { Field = nameof(p.IssuingAuthorithy), Error = "Kada se prijavljuje domaći gost, ne treba unositi polje 'Organ izdavanja putne isprave'." });

                if (p.EntryPlaceCode == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.EntryPlaceCode), Error = "Kada se prijavljuje strani gost, morate uneti podatak 'Šifra mesta ulaska u republiku Srbiju'." });
                if (p.EntryPlace == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.EntryPlace), Error = "Kada se prijavljuje strani gost, morate uneti podatak 'Mesto ulaska u republiku Srbiju'." });
                //if (p.StayValidTo != null) err.Errors.Add(new PersonFieldError() { Field = nameof(p.StayValidTo), Error = "Kada se prijavljuje domaći gost, ne treba unositi polje 'Datum do kada je odobren boravak u republici Srbiji'." });

                if (p.VisaType != null)
                {
                    if (p.VisaNumber != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.VisaNumber), Error = "Kada se prijavljuje strani gost, i kada ste uneli podatak 'Vrsta vize', morate uneti podatak 'Broj vize'." });
                    if (p.VisaIssuingPlace != null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.VisaIssuingPlace), Error = "Kada se prijavljuje strani gost, i kada ste uneli podatak 'Vrsta vize', morate uneti podatak 'Mesto izdavanja vize'." });
                }
            }
        }

        return err;
    }

    public async override Task<object> Properties(LegalEntity legalEntity)
    {
        await Authenticate(legalEntity);

        var pr = new ObjektiRequest();
        await RefreshToken();
        var token_decoded = new JwtSecurityTokenHandler().ReadToken(_token) as JwtSecurityToken;
        var id = token_decoded.Claims.Where(a => a.Type == "DodatnoPolje1").FirstOrDefault().Value;
        pr.ownerJmbg = 1;
        pr.pageSize = 1000;
        pr.pageIndex = 0;
        pr.ugostiteljId = id;
        try
        {
            var propertiesEndpoint = _configuration["SRB:Endpoints:Properties"]!.Trim('/');
            var request = new RestRequest(propertiesEndpoint, Method.Post);
            request.AddHeader("Authorization", $"Bearer {_token}");
            request.AddHeader("RefreshToken", $"{_refreshToken}");
            var json = JsonSerializer.Serialize(pr);
            request.AddJsonBody(json);
            var response = await _client.ExecutePostAsync(request);
            var result = JsonSerializer.Deserialize<ObjektiResponse>(response.Content!)!;

            foreach (var o in result.objekti)
            {
                if (_db.Properties.Any(a => a.LegalEntityId == _legalEntity.Id && a.ExternalId == int.Parse(o.idObjekta)) == false)
                {
                    if (o.sifraStatusa == "1")
                    {
                        var property = new Property();
                        property.LegalEntityId = _legalEntity.Id;
                        property.ExternalId = int.Parse(o.idObjekta);
                        property.Type = o.vrstaObjekta.ToString();
                        property.Address = o.adresa;                        
                        property.RegNumber = o.brojResenja;
                        property.Status = o.sifraStatusa;
                        property.Name = o.nazivObjekta;
                        property.PropertyName = o.nazivObjekta;
                        _db.Properties.Add(property);
                        _db.SaveChanges();
                    }
                }
            }

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError($"ERROR SRB:Login {e.Message}");
            _logger.LogTrace($"POST CHECK OUT {JsonSerializer.Serialize(pr)}");
            throw;
        }
    }
}