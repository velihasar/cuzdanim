using System.Threading.Tasks;

namespace Business.Services
{
    public interface IFirebaseNotificationService
    {
        Task<bool> SendNotificationAsync(string fcmToken, string title, string body, object data = null);
        Task<bool> SendNotificationToMultipleAsync(string[] fcmTokens, string title, string body, object data = null);
    }
}

