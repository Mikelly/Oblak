using Oblak.Data;

namespace Oblak.Models
{
    public class PersonViewModel
    {        
        public List<CodeList> PersonTypeCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> CountryCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> DocumentTypeCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> EntryPointCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> VisaTypeCodeList { get; set; } = new List<CodeList>();
    }
}
