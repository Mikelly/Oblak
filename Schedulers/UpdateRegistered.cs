using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;

namespace Oblak.Schedulers
{
    public class UpdateRegisteredScheduler
    {
        ILogger<UpdateRegisteredScheduler> _logger;
        ApplicationDbContext _db;
        IServiceScopeFactory _scopeFactory;
        Register _registerClient;        

        public UpdateRegisteredScheduler(ILogger<UpdateRegisteredScheduler> logger, ApplicationDbContext db, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _db = db;            
            _scopeFactory = scopeFactory;
        }

        public async Task Update()
        {
            var legalentities = _db.LegalEntities.Include(a => a.Partner).Include(a => a.Properties)
                                .Where(a => a.Partner.CheckRegistered)
                                .ToList();

            foreach (var le in legalentities)
            { 
                if(le.Properties.Any(a => a.RegDate < DateTime.Now || a.RegDate == null)) le.IsRegistered = false;
                else le.IsRegistered = true;
            }

            _db.SaveChanges();

        }
    }
}
