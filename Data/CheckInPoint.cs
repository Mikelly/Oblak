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
<<<<<<< HEAD
                
        public int PartnerId { get; set; }
=======

        [StringLength(450)]
        public int LegalEntityId { get; set; }
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144

        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(2000)]
        public string Address { get; set; }

<<<<<<< HEAD
        public Partner Partner { get; set; }

        [StringLength(450)]
        public string? Location { get; set; }

        [StringLength(450)]
        public string? Type { get; set; }

        [StringLength(450)]
        public string? Status { get; set; }
=======
        public LegalEntity LegalEntity { get; set; }
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144


        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }
<<<<<<< HEAD
        
        public DateTime? UserCreatedDate { get; set; }
        [StringLength(450)]
        
        public string? UserModified { get; set; }
        
=======
        public DateTime? UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
        public DateTime? UserModifiedDate { get; set; }

        #endregion
    }
}