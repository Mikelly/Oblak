namespace Oblak.Models.Api
{
    public class MrzDto
    {
		public string? DocType { get; set; }
		public string? DocNumber { get; set; }
		public string? DocExpiry { get; set; }
		public string? DocIssuer { get; set; }
		public string? DocAuthority { get; set; }
		public string? HolderNamePrimary { get; set; }
		public string? HolderNameSecondary { get; set; }
		public string? HolderNameTertiary { get; set; }
		public string? HolderNationality { get; set; }
		public string? HolderDateOfBirth { get; set; }
		public string? HolderSex { get; set; }
		public string? HolderNumber { get; set; }
	}
}
