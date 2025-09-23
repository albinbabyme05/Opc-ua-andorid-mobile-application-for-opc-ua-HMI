using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace CMLGapp.Services
{
    public class EmailService
    {
        private const string SenderEmail = "albinbaby150@gmail.com";
        private const string SenderPassword = "kgvruanbifhtfxbv";

        public static async Task SendResetCodeAsync(string recipientEmail, string code)
        {
            var mail = new MailMessage
            {
                From = new MailAddress(SenderEmail),
                Subject = "Your Password Reset Code",
                IsBodyHtml = true
            };
            mail.To.Add(recipientEmail);

            string htmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif; padding: 20px; background-color: #f9f9f9; color: #000;'>
                <div style='max-width: 500px; margin: auto;'>

                    <p>Hello {recipientEmail},</p>
                    <p>Someone tried to log in to your CMLG account.</p>

                    <p>If this was you, please use the following <span style='background-color: #f5f5a5;'>code</span> to change your password:</p>

                    <p style='font-size: 28px; font-weight: bold; color: #333;'>{code}</p>

                    <p>If it wasn't you, please <span style='background-color: #f5f5a5;'>reset</span> 
                    <a href='#' style='color:#007BFF;'>your password</a> to secure your account.</p>

                    <br/>
                    <p style='font-size: 13px; color: #777;'>Thank you,<br/>Zoller Support Team</p>

                </div>
            </body>
            </html>";

            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
            mail.AlternateViews.Add(htmlView);

            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(SenderEmail, SenderPassword),
                EnableSsl = true
            };

            await smtp.SendMailAsync(mail);
        }



    }
}
