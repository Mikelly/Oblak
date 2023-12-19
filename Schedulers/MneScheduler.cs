using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;

namespace Oblak.Schedulers
{
    public class MneScheduler
    {
        ILogger<MneScheduler> _logger;
        ApplicationDbContext _db;
        IServiceScopeFactory _scopeFactory;
        Register _registerClient;

        public MneScheduler(ILogger<MneScheduler> logger, ApplicationDbContext db, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _db = db;
            _scopeFactory = scopeFactory;
        }

        public async Task CleanUp()
        {
            var notSent = _db.MnePersons.Include(a => a.Group).ThenInclude(a => a.Property).ThenInclude(a => a.LegalEntity)
                .Where(a => a.ExternalId == null).GroupBy(a => a.Group.Property.LegalEntity).ToList();

            foreach (var g in notSent)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    _registerClient = scope.ServiceProvider.GetRequiredService<MneClient>();

                    await _registerClient.Authenticate(g.Key);

                    foreach (var p in g.ToList())
                    {
                        try
                        {
                            (_registerClient as MneClient).sendOne2Mup(p, false);
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError("SEND2MUP Error from Scheduler" + ex.Message);
                        }
                    }
                }
            }
        }
    }
}
