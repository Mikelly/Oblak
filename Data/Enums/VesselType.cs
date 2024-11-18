using System.ComponentModel.DataAnnotations;

namespace Oblak.Data.Enums
{
    public enum VesselType
    {
        [Display(Name = "Jahta")]
        Yacht,

        [Display(Name = "Brod")]
        Ship,
    }    
}
