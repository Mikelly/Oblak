using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Services.MNE;

namespace Oblak.HostedServices
{
    public class MneScheduler : IInvocable
    {
        ILogger<MneScheduler> _logger;
        ApplicationDbContext _db;
        IServiceScopeFactory _scopeFactory;
        MneClient _rb90Client;

        public MneScheduler(ILogger<MneScheduler> logger, ApplicationDbContext db, MneClient rb90Client, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _db = db;
            _rb90Client = rb90Client;
            _scopeFactory = scopeFactory;
        }

        public async Task Invoke()
        {
            //using (var scope = _scopeFactory.CreateScope())
            //{
            //    //var rb90Client = scope.ServiceProvider.GetRequiredService<RB90Client>();

            //    var rb90Client = ActivatorUtilities.CreateInstance<rb90Client>(scope.ServiceProvider, "test");

            //    var a = 10l;
            //    a++;
            //}

            //return;

            var notSent = _db.MnePersons.Include(a => a.Group).ThenInclude(a => a.Property).Where(a => a.ExternalId == null).GroupBy(a => a.Group.Property).ToList();

            foreach (var g in notSent)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var (user, so, cert) = _rb90Client.Auth(g.Key);

                    foreach (var p in g.ToList())
                    {
                        try
                        {
                            _rb90Client.sendOne2Mup(p, user, cert, false);
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
