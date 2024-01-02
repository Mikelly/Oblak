using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class UserDevice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
                
        [StringLength(450)]
        public string UserId { get; set; }

        [StringLength(450)]
        public string DeviceId { get; set; }
                
        [StringLength(450)]
        public string FcmToken { get; set; }

        public DateTime LastUpdated { get; set; }
    }

}
