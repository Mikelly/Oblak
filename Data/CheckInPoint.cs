using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Oblak.Data
{
    public class CheckInPoint
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }                
        public int PartnerId { get; set; }

        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(2000)]
        public string Address { get; set; }

        public Partner Partner { get; set; }

        [StringLength(450)]
        public string? Location { get; set; }

        [StringLength(450)]
        public string? Type { get; set; }

        [StringLength(450)]
        public string? Status { get; set; }


        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }        
        
        public DateTime? UserCreatedDate { get; set; }

        [StringLength(450)]        
        public string? UserModified { get; set; }
        
        public DateTime? UserModifiedDate { get; set; }

        #endregion
    }
}