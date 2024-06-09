namespace Oblak.Models.Api
{
    public class BasicDto
    {
        public int id { get; set; }
        public string info { get; set; }
        public string error { get; set; }
        public List<PersonValidationError> errors { get; set; }
    }
}
