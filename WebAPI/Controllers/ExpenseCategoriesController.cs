
using Business.Handlers.ExpenseCategories.Commands;
using Business.Handlers.ExpenseCategories.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Core.Entities.Concrete;
using System.Collections.Generic;
using Entities.Dtos.ExpenseCategory;

namespace WebAPI.Controllers
{
    /// <summary>
    /// ExpenseCategories If controller methods will not be Authorize, [AllowAnonymous] is used.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ExpenseCategoriesController : BaseApiController
    {
        ///<summary>
        ///List ExpenseCategories
        ///</summary>
        ///<remarks>ExpenseCategories</remarks>
        ///<return>List ExpenseCategories</return>
        ///<response code="200"></response>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ExpenseCategoryGetAllDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getall")]
        public async Task<IActionResult> GetList()
        {
            var result = await Mediator.Send(new GetExpenseCategoriesQuery());
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }

        ///<summary>
        ///It brings the details according to its id.
        ///</summary>
        ///<remarks>ExpenseCategories</remarks>
        ///<return>ExpenseCategories List</return>
        ///<response code="200"></response>  
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExpenseCategory))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getbyid")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await Mediator.Send(new GetExpenseCategoryQuery { Id = id });
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }

        /// <summary>
        /// Add ExpenseCategory.
        /// </summary>
        /// <param name="createExpenseCategory"></param>
        /// <returns></returns>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateExpenseCategoryCommand createExpenseCategory)
        {
            var result = await Mediator.Send(createExpenseCategory);
            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }

        /// <summary>
        /// Update ExpenseCategory.
        /// </summary>
        /// <param name="updateExpenseCategory"></param>
        /// <returns></returns>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateExpenseCategoryCommand updateExpenseCategory)
        {
            var result = await Mediator.Send(updateExpenseCategory);
            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }

        /// <summary>
        /// Delete ExpenseCategory.
        /// </summary>
        /// <param name="deleteExpenseCategory"></param>
        /// <returns></returns>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] DeleteExpenseCategoryCommand deleteExpenseCategory)
        {
            var result = await Mediator.Send(deleteExpenseCategory);
            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }
    }
}
