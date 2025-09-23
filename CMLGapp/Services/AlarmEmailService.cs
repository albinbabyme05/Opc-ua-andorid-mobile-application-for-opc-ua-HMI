using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CMLGapp.Services
{
    public partial class AlarmEmailService
    {
        
        private const string SenderEmail = "albinbaby150@gmail.com";
        private const string SenderPassword = "kgvruanbifhtfxbv";

        public static async Task SendAlarmAsync(string recipientEmail, string subject, string htmlBody)
        {
            var mail = new MailMessage
            {
                From = new MailAddress(SenderEmail, "CMLG Alarm Bot"),
                Subject = subject,
                IsBodyHtml = true
            };
            mail.To.Add(recipientEmail);

            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
            mail.AlternateViews.Add(htmlView);

            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(SenderEmail, SenderPassword),
                EnableSsl = true
            };
            await smtp.SendMailAsync(mail);
        }

        public static Task SendAlarmAsync( string recipientEmail,int alarmId, string errorName, string message, string occurredAt, string solution)
        {
            string body = $@"
            <html>
              <body style='font-family: Arial, sans-serif; padding: 20px; background-color: #f9f9f9; color: #000;'>
                <p>Hello {WebUtility.HtmlEncode(recipientEmail)},</p>
    
                
                <p><b>ID:</b> <span style='color:#FA7D7D;'>{alarmId}</span></p>
                <p><b>Error:</b> <span style='color:#FA7D7D;'>{WebUtility.HtmlEncode(errorName)}</span></p>
                <p><b>Message:</b> <span style='color:#FA7D7D;'>{WebUtility.HtmlEncode(message)}</span></p>
                <p><b>Occurred at:</b> <span style='color:#FA7D7D;'>{WebUtility.HtmlEncode(occurredAt)}</span></p>
    
                <hr style='margin:20px 0;'/>
    
                <p><b>Suggested solution</b></p>
                <p style='color:#64C87D;'>{WebUtility.HtmlEncode(solution)}</p>
    
                <br/>
                <p style='font-size:12px; color:#777;'>This is an automated email from CMLG system.</p>
              </body>
            </html>";


            return SendAlarmAsync(
                recipientEmail,
                subject: $">>CMLG<< {errorName} Alarm",
                htmlBody: body);
        }
    }
}
