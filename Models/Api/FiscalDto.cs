namespace Oblak.Models.Api
{
    public class FiscalDto
    {
        public int Id { get; set; }
        public int LegalEntityId { get; set; }
        public int PropertyId { get; set; }
        public string FiscalEnuCode { get; set; }
        public string BusinessUnitCode { get; set; }
        public string OperatorCode { get; set; }
        public double? AutoDeposit { get; set; }
    }
}
