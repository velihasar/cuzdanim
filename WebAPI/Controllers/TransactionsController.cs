
using Business.Handlers.Assets.Queries;
using Business.Handlers.Transactions.Commands;
using Business.Handlers.Transactions.Queries;
using Core.Utilities.Results;
using Core.Entities.Concrete;
using Entities.Dtos.Asset;
using Entities.Dtos.Transaction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Transactions If controller methods will not be Authorize, [AllowAnonymous] is used.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : BaseApiController
    {
        ///<summary>
        ///List Transactions
        ///</summary>
        ///<remarks>Transactions</remarks>
        ///<return>List Transactions</return>
        ///<response code="200"></response>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Transaction>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getall")]
        public async Task<IActionResult> GetList()
        {
            var result = await Mediator.Send(new GetTransactionsQuery());
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }

        ///<summary>
        ///It brings the details according to its id.
        ///</summary>
        ///<remarks>Transactions</remarks>
        ///<return>Transactions List</return>
        ///<response code="200"></response>  
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Transaction))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getbyid")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await Mediator.Send(new GetTransactionQuery { Id = id });
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }

        /// <summary>
        /// Add Transaction.
        /// </summary>
        /// <param name="createTransaction"></param>
        /// <returns></returns>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateTransactionCommand createTransaction)
        {
            var result = await Mediator.Send(createTransaction);
            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }

        /// <summary>
        /// Update Transaction.
        /// </summary>
        /// <param name="updateTransaction"></param>
        /// <returns></returns>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateTransactionCommand updateTransaction)
        {
            var result = await Mediator.Send(updateTransaction);
            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }

        /// <summary>
        /// Delete Transaction.
        /// </summary>
        /// <param name="deleteTransaction"></param>
        /// <returns></returns>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] DeleteTransactionCommand deleteTransaction)
        {
            var result = await Mediator.Send(deleteTransaction);
            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }

        ///<summary>
        ///List UserTotalBalanceWithCategory
        ///</summary>
        ///<remarks>UserTotalBalancesWithCategory</remarks>
        ///<return>List UserTotalBalancesWithCategory</return>
        ///<response code="200"></response>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserTotalBalanceWithCategoryDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getusertotalbalanceswithcategory")]
        public async Task<IActionResult> GetUserTotalBalancesWithCategory()
        {
            var result = await Mediator.Send(new GetUserTotalBalanceWithCategoryQuery());
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }

        ///<summary>
        ///Get Transactions by Income Category Id with Pagination
        ///</summary>
        ///<remarks>Returns paginated list of transactions filtered by income category id</remarks>
        ///<param name="incomecategoryId">Income Category Id</param>
        ///<param name="page">Page number (default: 1)</param>
        ///<param name="take">Number of records per page (default: 20)</param>
        ///<return>Paginated list of Transactions with Income Category</return>
        ///<response code="200"></response>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResult<IEnumerable<TransactionsWithIncomeCategoryDto>>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getbyincomecategoryid")]
        public async Task<IActionResult> GetByIncomeCategoryId(int incomecategoryId, int page = 1, int take = 5)
        {
            var result = await Mediator.Send(new GetTransactionWithIncomeCategoryQuery 
            { 
                IncomecategoryId = incomecategoryId,
                Page = page,
                Take = take
            });
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }

        ///<summary>
        ///Get Monthly Expense by Category
        ///</summary>
        ///<remarks>Returns list of expense categories with total amounts and percentages for current month</remarks>
        ///<return>List of MonthlyExpenseByCategoryDto</return>
        ///<response code="200"></response>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<MonthlyExpenseByCategoryDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getmonthlyexpensebycategory")]
        public async Task<IActionResult> GetMonthlyExpenseByCategory()
        {
            var result = await Mediator.Send(new GetMonthlyExpenseByCategoryQuery());
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }

        ///<summary>
        ///Get Expense Trend by Category
        ///</summary>
        ///<remarks>Returns list of expense categories with trend comparison between current month and previous month</remarks>
        ///<return>List of ExpenseTrendByCategoryDto</return>
        ///<response code="200"></response>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ExpenseTrendByCategoryDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getexpensetrendbycategory")]
        public async Task<IActionResult> GetExpenseTrendByCategory()
        {
            var result = await Mediator.Send(new GetExpenseTrendByCategoryQuery());
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }

        ///<summary>
        ///Get Top Expenses
        ///</summary>
        ///<remarks>Returns top 3 largest expense transactions for current month</remarks>
        ///<return>List of top 3 TopExpenseDto</return>
        ///<response code="200"></response>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TopExpenseDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("gettopexpenses")]
        public async Task<IActionResult> GetTopExpenses()
        {
            var result = await Mediator.Send(new GetTopExpensesQuery());
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }

        ///<summary>
        ///Get Expense Prediction
        ///</summary>
        ///<remarks>Returns predicted expense amount for current month based on historical data</remarks>
        ///<return>ExpensePredictionDto</return>
        ///<response code="200"></response>
        [Authorize]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExpensePredictionDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet("getexpenseprediction")]
        public async Task<IActionResult> GetExpensePrediction()
        {
            var result = await Mediator.Send(new GetExpensePredictionQuery());
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }
    }
}
