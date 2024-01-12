namespace Oblak.Models.Api
{
    public class LoginDto : BasicDto
    {
        public string auth { get; set; }
        public string sess { get; set; }
        public string oper { get; set; }
        public string lang { get; set; }
        public string cntr { get; set; }
        public List<string> roles { get; set; }
    }
}
