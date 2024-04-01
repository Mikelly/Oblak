namespace Oblak.Models.Api
{
    public class PersonErrorDto
    { 
        public string PersonId { get; set; }

        public List<PersonValidationError> ValidationErrors { get; set; } = new List<PersonValidationError>();

        public List<string> ExternalErrors { get; set; }
    }

    public class PersonValidationError
    { 
        public string Field { get; set; }
        public string Error { get; set; }
    }
}