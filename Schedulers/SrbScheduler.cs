using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;

namespace Oblak.Schedulers
{
    public class SrbScheduler
    {
        ILogger<SrbScheduler> _logger;
        ApplicationDbContext _db;
        IServiceScopeFactory _scopeFactory;
        Register _registerClient;        

        public SrbScheduler(ILogger<SrbScheduler> logger, ApplicationDbContext db, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _db = db;            
            _scopeFactory = scopeFactory;            
        }

        public async Task HourlyCheckOut()
        {
            var now = DateTime.Now;

            var notSent = _db.SrbPersons
                .Include(a => a.Group).ThenInclude(a => a.Property).ThenInclude(a => a.LegalEntity)
                .Where(a => a.CheckedIn == true && a.CheckedOut == false && a.ExternalId != 0)
                .Where(a => a.PlannedCheckOut != null && a.PlannedCheckOut <= now)
                .GroupBy(a => a.Group.Property.LegalEntity).ToList();

            foreach (var g in notSent)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    _registerClient = scope.ServiceProvider.GetRequiredService<SrbClient>();
                    
                    await _registerClient.Authenticate(g.Key);

                    foreach (var p in g.OrderByDescending(a => a.CheckIn).ToList())
                    {
                        try
                        {
                            p.CheckOut = DateTime.Now;
                            var result = await (_registerClient as SrbClient).CheckOut(p);                            
                            p.CheckedOut = true;
                            _db.SaveChanges();
                            await Task.Delay(100);
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError("SEND2MUP SRB Error from Scheduler CheckOut" + ex.Message);
                        }
                    }
                }
            }
        }

        public async Task CleanUp()
        {
            var now = DateTime.Now;

            var notSent = _db.SrbPersons
                .Include(a => a.Group).ThenInclude(a => a.Property).ThenInclude(a => a.LegalEntity)
                .Where(a => a.CheckedIn == false && a.CheckedOut == false && a.ExternalId == 0)                
                .GroupBy(a => a.Group.Property.LegalEntity).ToList();

            foreach (var g in notSent)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    _registerClient = scope.ServiceProvider.GetRequiredService<SrbClient>();

                    await _registerClient.Authenticate(g.Key);

                    foreach (var p in g.OrderByDescending(a => a.CheckIn).ToList())
                    {
                        try
                        {   
                            var result = await (_registerClient as SrbClient).CheckIn(p);
                            p.CheckedIn = true;
                            _db.SaveChanges();
                            await Task.Delay(100);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("SEND2MUP SRB Error from Scheduler CheckIn" + ex.Message);
                        }
                    }
                }
            }
        }
    }
}
