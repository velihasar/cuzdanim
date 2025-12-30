
using Business.BusinessAspects;
using Business.Constants;
using Business.Handlers.Transactions.ValidationRules;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Core.Entities.Concrete;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Transactions.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class CreateTransactionCommand : IRequest<IResult>
    {
        public int? AssetId { get; set; }
        public int? IncomeCategoryId { get; set; }
        public int? ExpenseCategoryId { get; set; }
        public decimal Amount { get; set; }
        public System.DateTime Date { get; set; }
        public string Description { get; set; }
        public Core.Enums.TransactionType Type { get; set; }
        public bool IsMonthlyRecurring { get; set; }
        public int? DayOfMonth { get; set; }
        public bool IsBalanceCarriedOver { get; set; }


        public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, IResult>
        {
            private readonly ITransactionRepository _transactionRepository;
            private readonly IMediator _mediator;
            public CreateTransactionCommandHandler(ITransactionRepository transactionRepository, IMediator mediator)
            {
                _transactionRepository = transactionRepository;
                _mediator = mediator;
            }

            [ValidationAspect(typeof(CreateTransactionValidator), Priority = 1)]
            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
            {
                
                var addedTransaction = new Transaction
                {
                    UserId = UserInfoExtensions.GetUserId(),
                    AssetId = request.AssetId,
                    IncomeCategoryId = request.IncomeCategoryId,
                    ExpenseCategoryId = request.ExpenseCategoryId,
                    Amount = request.Amount,
                    Date = request.Date,
                    Description = request.Description,
                    Type = request.Type,
                    IsMonthlyRecurring = request.IsMonthlyRecurring,
                    IsBalanceCarriedOver = request.IsBalanceCarriedOver,    
                    DayOfMonth = request.DayOfMonth,
                    CreatedBy = UserInfoExtensions.GetUserId(),
                    CreatedDate = DateTime.Now

                };

                _transactionRepository.Add(addedTransaction);
                await _transactionRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Added);
            }
        }
    }
}