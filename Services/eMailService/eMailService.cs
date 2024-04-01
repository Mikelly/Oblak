using SendGrid;
using SendGrid.Helpers.Mail;

namespace Oblak.Services
{
    public class eMailService
    {
        private ISendGridClient _sendGridClient;
        private IConfiguration _configuration;
        private EmailAddress _from;

        public eMailService(ISendGridClient sendGridClient, IConfiguration configuration)
        {
            _sendGridClient = sendGridClient;
            _configuration = configuration;
            _from = new EmailAddress(_configuration["SendGrid:EmailAddress"], _configuration["SendGrid:DisplayName"]);
        }

        public async void SendMailFromTemplate(string to, string templateId, object templateData)
        {
            var toEmailAddress = new EmailAddress(to, to);
            var msg = MailHelper.CreateSingleTemplateEmail(_from, toEmailAddress, templateId, templateData);            

            var response = await _sendGridClient.SendEmailAsync(msg);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Email has been sent successfully");
            }
            else
            {
                throw new Exception(response.StatusCode.ToString());
            }
        }

        public async Task SendMail(string from, string to, string templateId, object templateData, (string, Stream) attachment)
        {   
            var msg = new SendGridMessage();
            msg.SetFrom(new EmailAddress(from, from));
            msg.AddTo(new EmailAddress(to, to));
            msg.SetTemplateId(templateId);
            msg.SetTemplateData(templateData);
            await msg.AddAttachmentAsync(attachment.Item1, attachment.Item2);            
            var response = await _sendGridClient.SendEmailAsync(msg);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Email has been sent successfully");
            }
            else
            {
                throw new Exception(response.StatusCode.ToString());
            }
        }

        string Base64(Stream stream)
        {
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            return Convert.ToBase64String(bytes);
        }
    }
}
