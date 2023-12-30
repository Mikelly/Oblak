using AutoMapper;
using Kendo.Mvc.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.RulesetToEditorconfig;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Plugins;
using Oblak.Data;
using Oblak.Helpers;
using Oblak.Interfaces;
using Oblak.Models.Api;
using Oblak.Models.Srb;
using Oblak.Services.Payten;
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
        IHttpContextAccessor contextAccessor,
        IMapper mapper,
        eMailService eMailService,
        SelfRegisterService selfRegisterService,
        IWebHostEnvironment webHostEnvironment,
        IHubContext<MessageHub> messageHub,
        ApplicationDbContext db) 
        : 
        base(configuration, contextAccessor, eMailService, selfRegisterService, webHostEnvironment, messageHub, db)
    {
        _logger = logger;
        _mapper = mapper;

        var username = _context?.User?.Identity?.Name;
        if (username != null)
        {
            _user = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username)!;
            _legalEntity = _user?.LegalEntity!;
            if(_user != null && _legalEntity != null) SetUp(_user, _legalEntity);

            /*var test = _user.Type == Data.Enums.UserType.Test;
            var url = _configuration[$"SRB:{(test ? "TEST" : "PROD")}:URL"]!;
            var options = new RestClientOptions(url);
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.Converters.Add(new CustomDateTimeConverter("yyyy-MM-ddThh:mm:ss.000Z"));
            _client = new RestClient(options, configureSerialization: s => s.UseSystemTextJson(jsonOptions));
            _token = _user?.LegalEntity.SrbRbToken!;
            _refreshToken = _user?.LegalEntity.SrbRbRefreshToken!;
            */
        }
    }

    private void SetUp(ApplicationUser user, LegalEntity legalEntity)
    {   
        user = _db.Users.Where(a => a.LegalEntityId == legalEntity.Id).FirstOrDefault()!;
        var test = user.Type == Data.Enums.UserType.Test;
        var url = _configuration[$"SRB:{(test ? "TEST" : "PROD")}:URL"]!;
        var options = new RestClientOptions(url);
        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new CustomDateTimeConverter("yyyy-MM-ddThh:mm:ss.000Z"));
        _client = new RestClient(options, configureSerialization: s => s.UseSystemTextJson(jsonOptions));
        _token = legalEntity.SrbRbToken!;
        _refreshToken = legalEntity.SrbRbRefreshToken!;        
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
            (_user.LegalEntity.SrbRbToken, _user.LegalEntity.SrbRbRefreshToken) = (result.token, result.refreshToken);
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
                    await Login(new LoginRequest() { korisnickoIme = _user.LegalEntity.SrbRbUserName, lozinka = _user.LegalEntity.SrbRbPassword });
                }
            }
            else
            {
                await Login(new LoginRequest() { korisnickoIme = _user.LegalEntity.SrbRbUserName, lozinka = _user.LegalEntity.SrbRbPassword });
            }
            var refreshTokenEndpoint = _configuration["SRB:Endpoints:RefreshToken"]!.Trim('/');
            var request = new RestRequest(refreshTokenEndpoint, Method.Get);
            request.AddHeader("Authorization", $"Bearer {_token}");
            request.AddHeader("RefreshToken", $"{_refreshToken}");
            var response = await _client.ExecuteGetAsync(request);
            var jsonstring = JsonSerializer.Deserialize<string>(response.Content!);
            var result = JsonSerializer.Deserialize<LoginResponse>(jsonstring!)!;
            (_token, _refreshToken) = (result.token!, result.refreshToken!);
            (_user.LegalEntity.SrbRbToken, _user.LegalEntity.SrbRbRefreshToken) = (result.token, result.refreshToken);
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
            var test = _user.Type == Data.Enums.UserType.Test;
            var data = _db.SrbPersons.Where(a => a.GroupId == group.Id).ToList();

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
                await _messageHub.Clients.User(_context.User.Identity!.Name!).SendAsync("status",
                    (int)(Math.Round((decimal)c / (decimal)total * 100m, 0, MidpointRounding.AwayFromZero)),
                    $"Prijavljivanje gostiju {c}/{total}", $"{pr.FirstName} {pr.LastName}"
                    );

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

            await Persons(group);

            await _messageHub.Clients.User(_context.User.Identity!.Name!).SendAsync("status", 100, $"Prijavljivanje završeno");

            if (data.Any(a => a.Error != null))
            {
                foreach (var err in data.Where(a => a.Error != null))
                {
                    errors.Add(new PersonErrorDto() { PersonId = err.Id, ExternalErrors = err.Error != null ? JsonSerializer.Deserialize<List<string>>(err.Error)! : null });
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
        return null;
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
                DrzavaPrebivalistaAlfa2 = person.ResidenceCountryIso2,
                DrzavaPrebivalistaAlfa3 = person.ResidenceCountryIso3,
                MestoPrebivalistaMaticniBroj = person.ResidencePlaceCode,
                MestoPrebivalistaNaziv = person.ResidencePlaceName,
                OpstinaPrebivalistaMaticniBroj = person.ResidenceMunicipalityCode,
                OpstinaPrebivalistaNaziv = person.ResidenceMunicipalityName,
            },
            PodaciOBoravku = new podaciOBoravku
            { 
                UgostiteljskiObjekatJedinstveniIdentifikator = person.Group.Property.ExternalId.ToString(),
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
                MestoUlaskaURepublikuSrbiju = person.EntryPlace,
                MestoUlaskaURepublikuSrbijuSifra = person.EntryPlaceCode,
                DatumUlaskaURepublikuSrbiju = person.EntryDate.HasValue ? person.EntryDate.Value.ToString("yyyy-MM-dd") : null,
                DatumIzdavanjaPutneIsprave = person.DocumentIssueDate.HasValue ? person.DocumentIssueDate.Value.ToString("yyyy-MM-dd") : null,
                OrganIzdavanjaPutneIsprave = person.IssuingAuthorithy,
                DatumDoKadaJeOdobrenBoravakURepubliciSrbiji = person.StayValidTo,
                VrstaVizeSifra = person.VisaType,
                BrojVize = person.VisaNumber,
                MestoIzdavanjaVize = person.VisaIssuingPlace,
                Napomena = person.Note,
            }            
        };       

        return request;
    }


    public CheckOutRequest Person2CheckOutRequest(SrbPerson person)
    {
        CheckOutRequest request = new CheckOutRequest()
        {
            Izmena = person.CheckedOut ? true : false,
            DatumICasOdjave = (person.CheckOut ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm"),
            ExternalId = person.Guid,
            UgostiteljskiObjekatJedinstveniIdentifikator = person.Group.Property.ExternalId,
            BrojPruzenihUslugaSmestaja = person.NumberOfServices ?? 1            
        };

        return request;
    }

    public override Task<List<CodeList>> CodeLists()
    {
        return _db.CodeLists.Where(a => a.Country == Data.Enums.Country.SRB.ToString()).ToListAsync();
    }

    public override async Task<object> Authenticate(LegalEntity? legalEntity = null)
    {
        if (legalEntity != null)
        { 
            _legalEntity = legalEntity;
            _user = _db.Users.Include(a => a.LegalEntity).Where(a => a.LegalEntityId == _legalEntity.Id).FirstOrDefault()!;
             SetUp(_user, _legalEntity);
        }
         
        try
        {
            if (_token != null)
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_token);
                if (jwt.ValidTo < DateTime.Now)
                {
                    await Login(new LoginRequest() { korisnickoIme = _legalEntity.SrbRbUserName!, lozinka = _legalEntity.SrbRbPassword! });
                }
            }
            else
            {
                await Login(new LoginRequest() {korisnickoIme = _legalEntity.SrbRbUserName!, lozinka = _legalEntity.SrbRbPassword! });
            }
            var refreshTokenEndpoint = _configuration["SRB:Endpoints:RefreshToken"]!.Trim('/');
            var request = new RestRequest(refreshTokenEndpoint, Method.Get);
            request.AddHeader("Authorization", $"Bearer {_token}");
            request.AddHeader("RefreshToken", $"{_refreshToken}");
            var response = await _client.ExecuteGetAsync(request);
            var jsonstring = JsonSerializer.Deserialize<string>(response.Content!);
            var result = JsonSerializer.Deserialize<LoginResponse>(jsonstring!)!;
            (_token, _refreshToken) = (result.token!, result.refreshToken!);
            (_user.LegalEntity.SrbRbToken, _user.LegalEntity.SrbRbRefreshToken) = (result.token, result.refreshToken);
            _db.SaveChanges();
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError($"ERROR SRB:Refresh token {e.Message}");
            throw;
        }
    }

    public override async Task CertificateMail(Group group, string email)
    {
        throw new NotImplementedException();
    }

    public override async Task<Stream> CertificatePdf(Group group)
    {
        var ids = await Persons(group);
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

    public async Task<List<int>> Persons(Group group)
    {
        var ids = new List<int>();
        foreach (SrbPerson p in group.Persons)
        {
            try
            {
                if (p.ExternalId2 != null)
                {
                    ids.Add(p.ExternalId2!.Value);
                }
                else
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
                    if (result.totalRowsCount == 1)
                    {
                        p.ExternalId2 = result.data.First().turistaId;
                        _db.SaveChanges();
                        ids.Add(result.data.First().turistaId);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"ERROR SRB: Confirmation {e.Message}");                
                throw;
            }
        }
        return ids;
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

        dto.LegalEntityId = _user.LegalEntityId;        
                
        _mapper.Map(dto, srbPerson);
        
        _db.SaveChanges();        

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
            if (p.LegalEntity?.Type == Data.Enums.LegalEntityType.Company && p.ResidenceTaxDiscountReason == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ResidenceTaxDiscountReason), Error = "Morate uneti podatak 'Uslov za umanjenje boravišne takse'." });
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
                if (p.ExternalId.HasValue) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ExternalId), Error = "Podaci o stranom državljaninu se ne mogu menjati." });
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

    public async override Task<object> Properties()
    {
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
            var checkOutEndpoint = _configuration["SRB:Endpoints:Properties"]!.Trim('/');
            var request = new RestRequest(checkOutEndpoint, Method.Post);
            request.AddHeader("Authorization", $"Bearer {_token}");
            request.AddHeader("RefreshToken", $"{_refreshToken}");
            var json = JsonSerializer.Serialize(pr);
            request.AddJsonBody(json);
            var response = await _client.ExecutePostAsync(request);            
            var result = JsonSerializer.Deserialize<ObjektiResponse>(response.Content!)!;

            foreach (var o in result.objekti)
            {
                if (_db.Properties.Any(a => a.LegalEntityId == _user.LegalEntityId && a.ExternalId == int.Parse(o.idObjekta)) == false)
                {
                    var property = new Property();
                    property.LegalEntityId = _user.LegalEntityId;
                    property.ExternalId = int.Parse(o.idObjekta);
                    property.Type = o.vrstaObjekta.ToString();
                    property.Address = o.adresa;
                    property.Municipality = o.sifraOpstine;
                    property.RegNumber = o.brojResenja;
                    property.Status = o.sifraStatusa;
                    property.Name = o.nazivObjekta;
                    property.PropertyName = o.nazivObjekta;
                    _db.Properties.Add(property);
                    _db.SaveChanges();
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