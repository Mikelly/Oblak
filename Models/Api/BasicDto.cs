namespace Oblak.Models.Api
{
    public class BasicDto
    {
        public string info { get; set; }
        public string error { get; set; }
        public List<PersonValidationError> errors { get; set; }
    }
}
