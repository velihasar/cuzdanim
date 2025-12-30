
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


    public class UpdateTransactionCommand : IRequest<IResult>
    {
        public int Id { get; set; }
        public int? AssetId { get; set; }
        public int? IncomeCategoryId { get; set; }
        public int? ExpenseCategoryId { get; set; }
        public decimal Amount { get; set; }
        public System.DateTime Date { get; set; }
        public string Description { get; set; }
        public Core.Enums.TransactionType Type { get; set; }
        public bool IsMonthlyRecurring { get; set; }
        public bool IsBalanceCarriedOver { get; set; }
        public int? DayOfMonth { get; set; }

        public class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand, IResult>
        {
            private readonly ITransactionRepository _transactionRepository;
            private readonly IMediator _mediator;

            public UpdateTransactionCommandHandler(ITransactionRepository transactionRepository, IMediator mediator)
            {
                _transactionRepository = transactionRepository;
                _mediator = mediator;
            }

            [ValidationAspect(typeof(UpdateTransactionValidator), Priority = 1)]
            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
            {
                var isThereTransactionRecord = await _transactionRepository.GetAsync(u => u.Id == request.Id);

               
                isThereTransactionRecord.AssetId = request.AssetId;
                isThereTransactionRecord.IncomeCategoryId = request.IncomeCategoryId;
                isThereTransactionRecord.ExpenseCategoryId = request.ExpenseCategoryId;
                isThereTransactionRecord.Amount = request.Amount;
                isThereTransactionRecord.Date = request.Date;
                isThereTransactionRecord.Description = request.Description;
                isThereTransactionRecord.Type = request.Type;
                isThereTransactionRecord.IsMonthlyRecurring = request.IsMonthlyRecurring;
                isThereTransactionRecord.IsBalanceCarriedOver= request.IsBalanceCarriedOver;
                isThereTransactionRecord.DayOfMonth = request.DayOfMonth;
                isThereTransactionRecord.UpdatedBy = UserInfoExtensions.GetUserId();
                isThereTransactionRecord.UpdatedDate = DateTime.Now;


                _transactionRepository.Update(isThereTransactionRecord);
                await _transactionRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Updated);
            }
        }
    }
}

