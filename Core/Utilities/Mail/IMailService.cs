using System.Threading.Tasks;

namespace Core.Utilities.Mail
{
    public interface IMailService
    {
        Task SendAsync(EmailMessage emailMessage);
    }
}