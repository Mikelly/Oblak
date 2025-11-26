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
            _logger.LogInformation("SRB Scheduler: HourlyCheckOut started at {Time}", DateTime.Now);
            var startTime = DateTime.Now;
            var now = DateTime.Now;

            try
            {
                var notSent = _db.SrbPersons                
                    .Include(a => a.Property).ThenInclude(a => a.LegalEntity)                
                    .Where(a => a.CheckedIn == true && a.CheckedOut == false && a.ExternalId != 0)
                    .Where(a => a.PlannedCheckOut != null && a.PlannedCheckOut <= now)
                    .GroupBy(a => a.Property.LegalEntity).ToList();

                var totalPersons = notSent.Sum(g => g.Count());
                _logger.LogError("SRB Scheduler: Found {TotalPersons} person(s) to check out across {LegalEntityCount} legal entity(ies)", 
                    totalPersons, notSent.Count);

                if (totalPersons == 0)
                {
                    _logger.LogError("SRB Scheduler: No persons to check out");
                    return;
                }

                int successCount = 0;
                int errorCount = 0;

                foreach (var g in notSent)
                {
                    var legalEntityName = g.Key?.Name ?? "Unknown";
                    var legalEntityType = g.Key?.Type.ToString() ?? "Unknown";
                    var personsInGroup = g.Count();
                    _logger.LogError("SRB Scheduler: Processing {PersonCount} person(s) for legal entity: {LegalEntityName} | Type: {legalEntityType}", 
                        personsInGroup, legalEntityName, legalEntityType);

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        try
                        {
                            _registerClient = scope.ServiceProvider.GetRequiredService<SrbClient>();
                            
                            await _registerClient.Authenticate(g.Key);
                            _logger.LogError("SRB Scheduler: Authentication successful for legal entity: {LegalEntityName}", legalEntityName);

                            foreach (var p in g.OrderByDescending(a => a.CheckIn).ToList())
                            {
                                try
                                {
                                    _logger.LogError("SRB Scheduler: Processing checkout for {FirstName} {LastName} (ID: {PersonId}, ExternalId: {ExternalId})", 
                                        p.FirstName, p.LastName, p.Id, p.ExternalId);

                                    p.CheckOut = p.PlannedCheckOut;
                                    int days = (int)((p.CheckOut.Value.Date - p.CheckIn.Value.Date).TotalDays);

                                    p.NumberOfServices = p.NumberOfServices.HasValue 
                                                        && p.NumberOfServices.Value > 0
                                                        ? p.NumberOfServices
                                                        : Math.Max(days, 0);

                                    _logger.LogError("SRB Scheduler: Checkout details - CheckIn: {CheckIn}, CheckOut: {CheckOut}, Days: {Days}, NumberOfServices: {NumberOfServices}", 
                                        p.CheckIn, p.CheckOut, days, p.NumberOfServices);
                                     
                                    var result = await (_registerClient as SrbClient).CheckOut(p);
                                    if (result.errors.Any() == false)
                                    {
                                        p.CheckedOut = true;
                                        _db.SaveChanges();
                                        successCount++;
                                        _logger.LogError("SRB Scheduler: Successfully checked out {FirstName} {LastName} (ID: {PersonId})", 
                                            p.FirstName, p.LastName, p.Id);
                                    }
                                    else
                                    {
                                        p.CheckOut = null;
                                        p.CheckedOut = false;
                                        p.Error = string.Join(", ", result.errors);
                                        _db.SaveChanges();
                                        errorCount++;
                                        _logger.LogError("SRB Scheduler CheckOut Errors: {FirstName} {LastName} (ID: {PersonId}) - {Errors}", 
                                            p.FirstName, p.LastName, p.Id, string.Join(", ", result.errors));
                                    }

                                    await Task.Delay(100);
                                }
                                catch(Exception ex)
                                {
                                    errorCount++;
                                    _logger.LogError(ex, "SRB Scheduler: Error checking out person {FirstName} {LastName} (ID: {PersonId}) - {ErrorMessage}", 
                                        p.FirstName, p.LastName, p.Id, ex.Message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "SRB Scheduler: Error processing legal entity {LegalEntityName} - {ErrorMessage}", 
                                legalEntityName, ex.Message);
                        }
                    }
                }

                var duration = (DateTime.Now - startTime).TotalSeconds;
                _logger.LogInformation("SRB Scheduler: HourlyCheckOut completed - Duration: {Duration}s, Success: {SuccessCount}, Errors: {ErrorCount}, Total: {TotalPersons}", 
                    duration, successCount, errorCount, totalPersons);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SRB Scheduler: Fatal error in HourlyCheckOut - {ErrorMessage}", ex.Message);
                throw;
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
