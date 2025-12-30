
using Business.Handlers.Assets.Commands;
using Business.Handlers.Assets.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Core.Entities.Concrete;
using System.Collections.Generic;
using Entities.Dtos.Asset;
using System;
using System.Linq;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Assets If controller methods will not be Authorize, [AllowAnonymous] is used.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AssetsController : BaseApiController
    {
        ///<summary>
        ///List Assets
        ///</summary>
        ///<remarks>Assets</remarks>
        ///<return>List Assets</return>
        ///<response code="200"></response>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AssetGetAllDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getall")]
        
        public async Task<IActionResult> GetList()
        {
            var result = await Mediator.Send(new GetAssetsQuery());
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }

        ///<summary>
        ///It brings the details according to its id.
        ///</summary>
        ///<remarks>Assets</remarks>
        ///<return>Assets List</return>
        ///<response code="200"></response>  
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Asset))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getbyid")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await Mediator.Send(new GetAssetQuery { Id = id });
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }

        /// <summary>
        /// Add Asset.
        /// </summary>
        /// <param name="createAsset"></param>
        /// <returns></returns>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateAssetCommand createAsset)
        {
            var result = await Mediator.Send(createAsset);
            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }

        /// <summary>
        /// Update Asset.
        /// </summary>
        /// <param name="updateAsset"></param>
        /// <returns></returns>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateAssetCommand updateAsset)
        {
            var result = await Mediator.Send(updateAsset);
            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }

        /// <summary>
        /// Delete Asset.
        /// </summary>
        /// <param name="deleteAsset"></param>
        /// <returns></returns>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] DeleteAssetCommand deleteAsset)
        {
            var result = await Mediator.Send(deleteAsset);
            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }

        ///<summary>
        ///List Asset Debts
        ///</summary>
        ///<remarks>Asset Debts</remarks>
        ///<return>List Asset Debts</return>
        ///<response code="200"></response>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AssetDebtGetAllDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getalldebt")]
        public async Task<IActionResult> GetDebtList()
        {
            var result = await Mediator.Send(new GetDebtAssetsQuery());
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }

        ///<summary>
        ///Get Asset Distribution
        ///</summary>
        ///<remarks>Returns asset distribution by type (TL, Altın, Dolar, Euro, Borsa) with total value in TL</remarks>
        ///<return>AssetDistributionResultDto</return>
        ///<response code="200"></response>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AssetDistributionResultDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getassetdistribution")]
        public async Task<IActionResult> GetAssetDistribution()
        {
            var result = await Mediator.Send(new GetAssetDistributionQuery());
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }
    }
}
