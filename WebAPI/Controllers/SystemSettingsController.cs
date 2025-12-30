using System.Collections.Generic;
using System.Threading.Tasks;
using Business.Handlers.SystemSettings.Commands;
using Business.Handlers.SystemSettings.Queries;
using Core.Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    /// <summary>
    /// System Settings Controller - Admin Only
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SystemSettingsController : BaseApiController
    {
        /// <summary>
        /// List System Settings
        /// </summary>
        /// <param name="category">Optional: Filter by category</param>
        /// <returns>System Settings List</returns>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SystemSetting>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] string category = null)
        {
            return GetResponseOnlyResultData(await Mediator.Send(new GetSystemSettingsQuery { Category = category }));
        }

        /// <summary>
        /// Get System Setting by Id
        /// </summary>
        /// <param name="id">System Setting Id</param>
        /// <returns>System Setting</returns>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SystemSetting))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            return GetResponseOnlyResultData(await Mediator.Send(new GetSystemSettingQuery { Id = id }));
        }

        /// <summary>
        /// Get System Setting by Key
        /// </summary>
        /// <param name="key">System Setting Key</param>
        /// <returns>System Setting</returns>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SystemSetting))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("key/{key}")]
        public async Task<IActionResult> GetByKey([FromRoute] string key)
        {
            return GetResponseOnlyResultData(await Mediator.Send(new GetSystemSettingQuery { Key = key }));
        }

        /// <summary>
        /// Create System Setting
        /// </summary>
        /// <param name="createSystemSetting"></param>
        /// <returns></returns>
        [Authorize]
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateSystemSettingCommand createSystemSetting)
        {
            return GetResponseOnlyResultMessage(await Mediator.Send(createSystemSetting));
        }

        /// <summary>
        /// Update System Setting
        /// </summary>
        /// <param name="updateSystemSetting"></param>
        /// <returns></returns>
        [Authorize]
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateSystemSettingCommand updateSystemSetting)
        {
            return GetResponseOnlyResultMessage(await Mediator.Send(updateSystemSetting));
        }

        /// <summary>
        /// Delete System Setting
        /// </summary>
        /// <param name="id">System Setting Id</param>
        /// <returns></returns>
        [Authorize]
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            return GetResponseOnlyResultMessage(await Mediator.Send(new DeleteSystemSettingCommand { Id = id }));
        }
    }
}

