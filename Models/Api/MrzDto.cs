namespace Oblak.Models.Api
{
<<<<<<< HEAD
	public class MrzDto
	{
=======
    public class MrzDto
    {
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
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
<<<<<<< HEAD

		public DateTime DocExpiryDate()
		{
			if ((this.DocExpiry ?? "") != "")
			{
				var day = int.Parse(this.DocExpiry.Substring(4, 2));
				var month = int.Parse(this.DocExpiry.Substring(2, 2));
				var year = int.Parse(this.DocExpiry.Substring(0, 2));

				var currYear = int.Parse(DateTime.Now.ToString("yy"));
				var currEra = int.Parse(DateTime.Now.ToString("yyyy").Substring(0, 2));

				var finalYear = currEra.ToString("00") + year.ToString("00");

				return new DateTime(int.Parse(finalYear), month, day);
            }
			else
			{
				return DateTime.Now.AddDays(1);
			}
        }

        public DateTime HolderDateOfBirthDate()
        {
            if ((this.HolderDateOfBirth ?? "") != "")
            {
                var day = int.Parse(this.HolderDateOfBirth.Substring(4, 2));
                var month = int.Parse(this.HolderDateOfBirth.Substring(2, 2));
                var year = int.Parse(this.HolderDateOfBirth.Substring(0, 2));

                var currYear = int.Parse(DateTime.Now.ToString("yy"));
                var currEra = int.Parse(DateTime.Now.ToString("yyyy").Substring(0, 2));

                var finalYear = "";

                if (currYear < year) finalYear = (currEra - 1).ToString("00") + year.ToString("00");
                else finalYear = currEra.ToString("00") + year.ToString("00");

                return new DateTime(int.Parse(finalYear), month, day);
            }
            else
            {
                return DateTime.Now;
            }
        }

    }
=======
	}
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
}
