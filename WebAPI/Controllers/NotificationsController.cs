using Business.Handlers.Users.Commands;
using Core.Utilities.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Notifications Controller - Push notification işlemleri için
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : BaseApiController
    {
        /// <summary>
        /// Register FCM token for current user
        /// </summary>
        /// <param name="command">FCM token registration details</param>
        /// <returns>Success or error message</returns>
        [Authorize]
        [Consumes("application/json")]
        [HttpPost("register-token")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        public async Task<IActionResult> RegisterToken([FromBody] RegisterFcmTokenCommand command)
        {
            return GetResponseOnlyResultMessage(await Mediator.Send(command));
        }

        /// <summary>
        /// Send push notification to a specific user or all users
        /// </summary>
        /// <param name="command">Notification details</param>
        /// <returns>Success or error message</returns>
        [Authorize]
        [Consumes("application/json")]
        [HttpPost("send")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SendNotification([FromBody] SendPushNotificationCommand command)
        {
            return GetResponseOnlyResultMessage(await Mediator.Send(command));
        }
    }
}

