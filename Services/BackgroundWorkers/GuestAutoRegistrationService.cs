using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Models.Api;
using Oblak.Services.MNE;
using System.Text.Json;

namespace Oblak.Services.BackgroundWorkers
{
    public class GuestAutoRegistrationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GuestAutoRegistrationService> _logger;
        private readonly IConfiguration _configuration;
        private Timer _timer;

        public GuestAutoRegistrationService(
            IServiceProvider serviceProvider,
            ILogger<GuestAutoRegistrationService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration; 
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Check if service is enabled
            var isEnabled = _configuration.GetValue<bool>("MNE:AutoRegistration:Enabled", false);
            
            if (!isEnabled)
            {
                _logger.LogInformation("GuestAutoRegistrationService is disabled via configuration.");
                return Task.CompletedTask;
            }

            _logger.LogError("GuestAutoRegistrationService is starting.");

            // Read execution time from config (default: 23:00)
            var executionTime = _configuration["MNE:AutoRegistration:ExecutionTime"] ?? "23:00";

            if (!TimeSpan.TryParse(executionTime, out var configuredTime))
            {
                _logger.LogWarning($"Invalid execution time format in config: {executionTime}. Using default 23:00.");
                configuredTime = new TimeSpan(23, 0, 0);
            }

            // Calculate time until first execution
            var now = DateTime.Now;
            var firstRun = DateTime.Today.Add(configuredTime);
            if (now > firstRun)
            {
                firstRun = firstRun.AddDays(1);
            }

            var timeToGo = firstRun - now;
            _logger.LogError($"First execution scheduled at {firstRun}. Time until execution: {timeToGo}");

            // Create timer that fires once daily
            _timer = new Timer(
                DoWork,
                null,
                timeToGo,
                TimeSpan.FromDays(1));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            // Double-check if service is still enabled before execution
            var isEnabled = _configuration.GetValue<bool>("MNE:AutoRegistration:Enabled", false);
            
            if (!isEnabled)
            {
                _logger.LogInformation("GuestAutoRegistrationService execution skipped - service is disabled.");
                return;
            }

            _logger.LogError("GuestAutoRegistrationService is executing.");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Set background service context BEFORE getting MneClient
                var backgroundContext = scope.ServiceProvider.GetRequiredService<IBackgroundServiceContext>();
                backgroundContext.BackgroundServiceUsername = _configuration["MNE:AutoRegistration:SystemUsername"] ?? "to.budva";
                
                var mneClient = scope.ServiceProvider.GetRequiredService<MneClient>();
                  
                var nowDate = DateTime.Now.Date;
                var twoDaysAgo = nowDate.AddDays(-2);

                // Get all unregistered guests for PartnerId = 4
                var unregisteredGuests = await db.MnePersons
                    .Include(p => p.Property).ThenInclude(p => p.LegalEntity)
                    .Include(p => p.Property).ThenInclude(p => p.Municipality)
                    .Include(p => p.LegalEntity)
                    .Include(p => p.Group)
                    .Where(p => p.ExternalId == null
                        && p.LegalEntity.PartnerId == 4
                        && p.IsDeleted == false
                        && p.CheckIn.Date >= twoDaysAgo
                        && p.CheckIn.Date <= nowDate)
                    .ToListAsync();

                _logger.LogError($"Found {unregisteredGuests.Count} unregistered guests to process.");

                int successCount = 0;
                int errorCount = 0;

                foreach (var person in unregisteredGuests)
                {
                    try
                    {
                        // Initialize MNE client with legal entity
                        await mneClient.Initialize(person.LegalEntity);

                        // Attempt registration
                        var result = await mneClient.RegisterPerson(person, null, null);

                        var log = new GuestAutoRegistrationLog
                        {
                            MnePersonId = person.Id,
                            ExternalId = person.ExternalId,
                            LegalEntityId = person.LegalEntityId,
                            PropertyId = person.PropertyId,
                            GroupId = person.GroupId,
                            CheckOut = person.CheckOut,
                            Success = result == null || (!result.ValidationErrors.Any() && !result.ExternalErrors.Any()),
                            InitializedBy = "system",
                            ValidationErrors = result?.ValidationErrors.Any() == true 
                                ? JsonSerializer.Serialize(result.ValidationErrors) 
                                : null,
                            ExternalErrors = result?.ExternalErrors.Any() == true 
                                ? JsonSerializer.Serialize(result.ExternalErrors) 
                                : null,
                            CreatedDate = DateTime.Now
                        };

                        db.GuestAutoRegistrationLogs.Add(log);
                        await db.SaveChangesAsync();

                        if (log.Success)
                        {
                            successCount++;
                            _logger.LogError($"Successfully registered guest ID: {person.Id}, Name: {person.FirstName} {person.LastName}, ExternalId: {person.ExternalId}");
                        }
                        else
                        {
                            errorCount++;
                            _logger.LogWarning($"Failed to register guest ID: {person.Id}, Name: {person.FirstName} {person.LastName}. Errors logged.");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, $"Exception while registering guest ID: {person.Id}, Name: {person.FirstName} {person.LastName}");

                        // Log the exception
                        var log = new GuestAutoRegistrationLog
                        {
                            MnePersonId = person.Id,
                            ExternalId = null,
                            LegalEntityId = person.LegalEntityId,
                            PropertyId = person.PropertyId,
                            GroupId = person.GroupId,
                            CheckOut = person.CheckOut,
                            Success = false,
                            InitializedBy = "system",
                            ValidationErrors = null,
                            ExternalErrors = JsonSerializer.Serialize(new[] { ex.Message }),
                            CreatedDate = DateTime.Now
                        };

                        db.GuestAutoRegistrationLogs.Add(log);
                        await db.SaveChangesAsync();
                    }
                }

                _logger.LogError($"GuestAutoRegistrationService completed. Success: {successCount}, Errors: {errorCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in GuestAutoRegistrationService");
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}
