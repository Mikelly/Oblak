namespace Oblak.Models.Api
{
    public class PersonErrorDto
    { 
        public int PersonId { get; set; }

        public List<PersonError> Errors { get; set; } = new List<PersonError>();
    }

    public class PersonError
    { 
        public string Field { get; set; }
        public string Error { get; set; }
    }
}