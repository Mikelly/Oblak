using Oblak.Data;

namespace Oblak.Models
{
    public class CodeListViewModel
    {
        public List<CodeList> GenderCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> PersonTypeCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> CountryCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> MunicipalityCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> PlaceCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> DocumentTypeCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> EntryPointCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> VisaTypeCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> ServiceTypeCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> ArrivalTypeCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> ReasonForStayCodeList { get; set; } = new List<CodeList>();
        public List<CodeList> DiscountReasonCodeList { get; set; } = new List<CodeList>();
        public Dictionary<string, string> ResTaxPaymentTypes { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ResTaxTypes { get; set; } = new Dictionary<string, string>();
    }
}
