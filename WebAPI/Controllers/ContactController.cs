using Business.Handlers.Contact.Commands;
using Core.Utilities.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Contact Controller - İletişim formu mesajları için
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : BaseApiController
    {
        /// <summary>
        /// Send contact form message
        /// </summary>
        /// <param name="command">Contact message details</param>
        /// <returns>Success or error message</returns>
        [Authorize]
        [Consumes("application/json")]
        [HttpPost("send")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SendContactMessage([FromBody] SendContactMessageCommand command)
        {
            return GetResponseOnlyResultMessage(await Mediator.Send(command));
        }
    }
}

