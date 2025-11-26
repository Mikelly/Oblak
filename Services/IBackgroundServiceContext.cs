namespace Oblak.Services
{
    public interface IBackgroundServiceContext
    {
        /// <summary>
        /// Username korisnika koji se koristi u background servisu.
        /// </summary>
        string? BackgroundServiceUsername { get; set; }
        
        /// <summary>
        /// Indikator da li se trenutno izvrsava background service.
        /// </summary>
        bool IsBackgroundService { get; }
    }

    /// <summary>
    /// Implementacija background service context-a
    /// Registruje se kao SCOPED servis
    /// </summary>
    public class BackgroundServiceContext : IBackgroundServiceContext
    {
        public string? BackgroundServiceUsername { get; set; }
        
        public bool IsBackgroundService => !string.IsNullOrEmpty(BackgroundServiceUsername);
    }
}
