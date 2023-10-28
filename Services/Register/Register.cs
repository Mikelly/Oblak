using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Interfaces;
using Oblak.Models.Api;
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
        protected ApplicationUser _user;
        protected eMailService _eMailService;
        protected SelfRegisterService _selfRegisterService;
        protected IWebHostEnvironment _webHostEnvironment;
        protected IHubContext<MessageHub> _messageHub;        

        public Register(            
            IConfiguration configuration,
            IHttpContextAccessor contextAccessor,
            eMailService eMailService,
            SelfRegisterService selfRegisterService,
            IWebHostEnvironment webHostEnvironment,
            IHubContext<MessageHub> messageHub,
            ApplicationDbContext db)
        {            
            _configuration = configuration;
            _db = db;
            _context = contextAccessor.HttpContext;
            _eMailService = eMailService;
            _selfRegisterService = selfRegisterService;
            _webHostEnvironment = webHostEnvironment;
            _messageHub = messageHub;

            var username = _context.User.Identity.Name;
            if (username != null)
            {
                _user = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username)!;
            }
        }

        
        #region ABSTRACT METHODS

        public abstract Task<Person> Person(object person);

        public abstract Task<List<PersonErrorDto>> RegisterGroup(Group group, DateTime? checkInDate, DateTime? checkOutDate);

        public abstract Task<PersonErrorDto> RegisterPerson(Person person, DateTime? checkInDate, DateTime? checkOutDate);

        public abstract List<PersonErrorDto> Validate(Group group, DateTime? checkInDate, DateTime? checkOutDate);
        
        public abstract PersonErrorDto Validate(Person person, DateTime? checkInDate, DateTime? checkOutDate);

        public abstract Task<List<CodeList>> CodeLists();

        public abstract Task<object> Authenticate();

        public abstract Task<object> Properties();

        public abstract Task CertificateMail(Group group, string email);

        public abstract Task<Stream> CertificatePdf(Group group);

        public abstract Task SendGuestToken(int propertyId, int? unitId, string email, string phoneNo, string lang);

        #endregion

        #region COMMON METHODS

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
            m.LegalEntityId = _user.LegalEntity.Id;
            m.PropertyId = group.PropertyId;
            m.PropertyExternalId = property.ExternalId;
            m.Email = group.Email;
            if (m.CheckIn.HasValue) m.CheckIn = group.CheckIn.Value; else m.CheckIn = DateTime.Now;
            if (m.CheckOut.HasValue) m.CheckOut = group.CheckOut.Value;
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

        #endregion
    }
}
