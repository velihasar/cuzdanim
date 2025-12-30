using Business.BusinessAspects;
using Business.Constants;
using Business.Handlers.Contact.ValidationRules;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Mail;
using Core.Utilities.Results;
using MediatR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Contact.Commands
{
    public class SendContactMessageCommand : IRequest<IResult>
    {
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }

    public class SendContactMessageCommandHandler : IRequestHandler<SendContactMessageCommand, IResult>
    {
        private readonly IMailService _mailService;
        private readonly IConfiguration _configuration;

        public SendContactMessageCommandHandler(
            IMailService mailService,
            IConfiguration configuration)
        {
            _mailService = mailService;
            _configuration = configuration;
        }

        [ValidationAspect(typeof(SendContactMessageValidator), Priority = 1)]
        [SecuredOperation(Priority = 1)]
        [LogAspect(typeof(FileLogger))]
        public async Task<IResult> Handle(SendContactMessageCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                return new ErrorResult("Konu gereklidir.");
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return new ErrorResult("Mesaj gereklidir.");
            }

            try
            {
                var senderEmail = _configuration.GetSection("EmailConfiguration").GetSection("SenderEmail").Value;
                var senderName = _configuration.GetSection("EmailConfiguration").GetSection("SenderName").Value;
                
                if (string.IsNullOrEmpty(senderEmail))
                {
                    senderEmail = "noreply@cuzdanim.com";
                }
                
                if (string.IsNullOrEmpty(senderName))
                {
                    senderName = "Cüzdanım";
                }

                // Support email adresi (alıcı)
                var supportEmail = "support@masavtech.com";
                var supportName = senderName + " Support";
                
                // Uygulama adı (mail içinde gösterilecek)
                var appName = senderName;

                // Kullanıcıdan gelen mesajı admin'e gönder
                var emailContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0a7ea4; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .info {{ background-color: #fff; padding: 15px; margin: 15px 0; border-left: 4px solid #0a7ea4; }}
        .message {{ background-color: #fff; padding: 20px; margin: 15px 0; border-radius: 5px; }}
        .app-info {{ background-color: #e3f2fd; padding: 10px; margin: 15px 0; border-radius: 5px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Yeni İletişim Formu Mesajı</h2>
        </div>
        <div class='content'>
            <div class='app-info'>
                <strong>Uygulama:</strong> {appName}
            </div>
            <div class='info'>
                <strong>Kullanıcı Adı:</strong> {request.UserName ?? "Belirtilmemiş"}<br>
                <strong>E-posta:</strong> {request.UserEmail ?? "Belirtilmemiş"}<br>
                <strong>Konu:</strong> {request.Subject}
            </div>
            <div class='message'>
                <strong>Mesaj:</strong><br>
                {request.Message.Replace("\n", "<br>")}
            </div>
        </div>
    </div>
</body>
</html>";

                var emailMessage = new EmailMessage
                {
                    ToAddresses = new List<EmailAddress> 
                    { 
                        new EmailAddress 
                        { 
                            Name = supportName, 
                            Address = supportEmail 
                        } 
                    },
                    FromAddresses = new List<EmailAddress> 
                    { 
                        new EmailAddress 
                        { 
                            Name = senderName, 
                            Address = senderEmail 
                        } 
                    },
                    ReplyToAddresses = !string.IsNullOrWhiteSpace(request.UserEmail) 
                        ? new List<EmailAddress> 
                        { 
                            new EmailAddress 
                            { 
                                Name = request.UserName ?? "Kullanıcı", 
                                Address = request.UserEmail 
                            } 
                        }
                        : new List<EmailAddress>(),
                    Subject = $"{appName} - İletişim Formu: {request.Subject}",
                    Content = emailContent
                };

                await _mailService.SendAsync(emailMessage);

                return new SuccessResult("Mesajınız başarıyla gönderildi.");
            }
            catch (Exception ex)
            {
                return new ErrorResult("Mesaj gönderilirken bir hata oluştu: " + ex.Message);
            }
        }
    }
}

