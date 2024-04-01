using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Interfaces;
using Oblak.Models.Api;
using Oblak.Services.MNE;
using Oblak.Services.SRB;
using Oblak.SignalR;
using RB90;
using RestSharp;

namespace Oblak.Services
{
    public abstract class Register
    {         
        protected IConfiguration _configuration;
        protected HttpContext _context;
        protected ApplicationDbContext _db;        
        protected eMailService _eMailService;
        protected SelfRegisterService _selfRegisterService;
<<<<<<< HEAD
        protected IWebHostEnvironment _env;
=======
        protected IWebHostEnvironment _webHostEnvironment;
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
        protected IHubContext<MessageHub> _messageHub;
        protected LegalEntity _legalEntity;

        public Register(            
            IConfiguration configuration,            
            eMailService eMailService,
            SelfRegisterService selfRegisterService,
            IWebHostEnvironment webHostEnvironment,
            IHubContext<MessageHub> messageHub,
            ApplicationDbContext db)
        {            
            _configuration = configuration;
            _db = db;            
            _eMailService = eMailService;
            _selfRegisterService = selfRegisterService;
<<<<<<< HEAD
            _env = webHostEnvironment;
            _messageHub = messageHub;            
=======
            _webHostEnvironment = webHostEnvironment;
            _messageHub = messageHub;
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
        }

        
        #region ABSTRACT METHODS

        public abstract Task<Person> Person(object person);

		public abstract Task<Person> PersonFromMrz(MrzDto mrz);

		public abstract Task<List<PersonErrorDto>> RegisterGroup(Group group, DateTime? checkInDate, DateTime? checkOutDate);

        public abstract Task<PersonErrorDto> RegisterPerson(Person person, DateTime? checkInDate, DateTime? checkOutDate);

        public abstract List<PersonErrorDto> Validate(Group group, DateTime? checkInDate, DateTime? checkOutDate);
        
        public abstract PersonErrorDto Validate(Person person, DateTime? checkInDate, DateTime? checkOutDate);

        public abstract Task<List<CodeList>> CodeLists();

        public abstract Task<object> Authenticate(LegalEntity? legalEntity = null);

        public abstract Task<object> Properties(LegalEntity legalEntity);

<<<<<<< HEAD
		public abstract Task ConfirmationGroupMail(Group group, string email);

        public abstract Task<Stream> ConfirmationGroupPdf(Group group);

        public abstract Task ConfirmationPersonMail(Person person, string email);

        public abstract Task<Stream> ConfirmationPersonPdf(Person person);
=======
		public abstract Task CertificateMail(Group group, string email);

        public abstract Task<Stream> CertificatePdf(Group group);        
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144

        public abstract Task SendGuestToken(int propertyId, int? unitId, string email, string phoneNo, string lang);

        #endregion

        #region COMMON METHODS

        public async Task Initialize(LegalEntity legalEntity)
        { 
            this._legalEntity = legalEntity;
        }

        public async Task<List<Property>> GetProperties()
		{
			var data = _db.Properties.Where(a => a.LegalEntityId == _legalEntity.Id).ToList();
			var ids = _db.LegalEntities.Where(a => a.AdministratorId == _legalEntity.Id).Select(a => a.Id).ToList();
			var data1 = _db.Properties.Include(a => a.LegalEntity).Where(a => ids.Contains(a.LegalEntityId)).ToList();

			data.AddRange(data1);

			return data;
		}

		public async Task<List<LegalEntity>> GetLegalEntities()
		{			
			var ids = _db.LegalEntities.Where(a => a.AdministratorId == _legalEntity.Id).Select(a => a.Id).ToList();
			var data = _db.LegalEntities.Where(a => ids.Contains(a.Id)).ToList();

			data.Add(_legalEntity);

			return data;
		}

		public async Task<Group> Group(GroupDto group)
        {
            Group m = null;
            if (group.Id == 0)
            {
                m = new Group();
                _db.Groups.Add(m);
            }
            else
            {
                m = _db.Groups.SingleOrDefault(a => a.Id == group.Id)!;
            }

            var property = _db.Properties.FirstOrDefault(p => p.Id == group.PropertyId);
            m.LegalEntityId = property.LegalEntityId;
            m.PropertyId = group.PropertyId;
            m.PropertyExternalId = property.ExternalId;
            m.Email = group.Email;
            if (group.CheckIn.HasValue) m.CheckIn = group.CheckIn.Value; else m.CheckIn = DateTime.Now;
            if (group.CheckOut.HasValue) m.CheckOut = group.CheckOut.Value;
            m.Date = DateTime.Now;
            m.Guid = Guid.NewGuid().ToString();
            m.Status = "A";

            _db.SaveChanges();

            return m;
        }

        public Task<BasicDto> GroupDelete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<BasicDto> PersonDelete(Person person)
        {
            try
            {
                if (person is SrbPerson)
                {
                    _db.SrbPersons.Remove(person as SrbPerson);
                }

                if (person is MnePerson)
                {
                    _db.MnePersons.Remove(person as MnePerson);
                }
                        
                await _db.SaveChangesAsync();

                return new BasicDto() { info = "Gost je obrisan!", error = "" };
            }
            catch(Exception ex)
            {
                return new BasicDto() { info = "", error = ex.Message };
            }
        }

        #endregion
    }
}
