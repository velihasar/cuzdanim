
using Business.Handlers.Transactions.Queries;
using DataAccess.Abstract;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static Business.Handlers.Transactions.Queries.GetTransactionQuery;
using Entities.Concrete;
using static Business.Handlers.Transactions.Queries.GetTransactionsQuery;
using static Business.Handlers.Transactions.Commands.CreateTransactionCommand;
using Business.Handlers.Transactions.Commands;
using Business.Constants;
using static Business.Handlers.Transactions.Commands.UpdateTransactionCommand;
using static Business.Handlers.Transactions.Commands.DeleteTransactionCommand;
using MediatR;
using System.Linq;
using FluentAssertions;
using Core.Entities.Concrete;


namespace Tests.Business.HandlersTest
{
    [TestFixture]
    public class TransactionHandlerTests
    {
        Mock<ITransactionRepository> _transactionRepository;
        Mock<IMediator> _mediator;
        [SetUp]
        public void Setup()
        {
            _transactionRepository = new Mock<ITransactionRepository>();
            _mediator = new Mock<IMediator>();
        }

        [Test]
        public async Task Transaction_GetQuery_Success()
        {
            //Arrange
            var query = new GetTransactionQuery();

            _transactionRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<Transaction, bool>>>())).ReturnsAsync(new Transaction()
//propertyler buraya yazılacak
//{																		
//TransactionId = 1,
//TransactionName = "Test"
//}
);

            var handler = new GetTransactionQueryHandler(_transactionRepository.Object, _mediator.Object);

            //Act
            var x = await handler.Handle(query, new System.Threading.CancellationToken());

            //Asset
            x.Success.Should().BeTrue();
            //x.Data.TransactionId.Should().Be(1);

        }

        [Test]
        public async Task Transaction_GetQueries_Success()
        {
            //Arrange
            var query = new GetTransactionsQuery();

            _transactionRepository.Setup(x => x.GetListAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                        .ReturnsAsync(new List<Transaction> { new Transaction() { /*TODO:propertyler buraya yazılacak TransactionId = 1, TransactionName = "test"*/ } });

            var handler = new GetTransactionsQueryHandler(_transactionRepository.Object, _mediator.Object);

            //Act
            var x = await handler.Handle(query, new System.Threading.CancellationToken());

            //Asset
            x.Success.Should().BeTrue();
            ((List<Transaction>)x.Data).Count.Should().BeGreaterThan(1);

        }

        [Test]
        public async Task Transaction_CreateCommand_Success()
        {
            Transaction rt = null;
            //Arrange
            var command = new CreateTransactionCommand();
            //propertyler buraya yazılacak
            //command.TransactionName = "deneme";

            _transactionRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                        .ReturnsAsync(rt);

            _transactionRepository.Setup(x => x.Add(It.IsAny<Transaction>())).Returns(new Transaction());

            var handler = new CreateTransactionCommandHandler(_transactionRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            _transactionRepository.Verify(x => x.SaveChangesAsync());
            x.Success.Should().BeTrue();
            x.Message.Should().Be(Messages.Added);
        }

        [Test]
        public async Task Transaction_CreateCommand_NameAlreadyExist()
        {
            //Arrange
            var command = new CreateTransactionCommand();
            //propertyler buraya yazılacak 
            //command.TransactionName = "test";

            _transactionRepository.Setup(x => x.Query())
                                           .Returns(new List<Transaction> { new Transaction() { /*TODO:propertyler buraya yazılacak TransactionId = 1, TransactionName = "test"*/ } }.AsQueryable());

            _transactionRepository.Setup(x => x.Add(It.IsAny<Transaction>())).Returns(new Transaction());

            var handler = new CreateTransactionCommandHandler(_transactionRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            x.Success.Should().BeFalse();
            x.Message.Should().Be(Messages.NameAlreadyExist);
        }

        [Test]
        public async Task Transaction_UpdateCommand_Success()
        {
            //Arrange
            var command = new UpdateTransactionCommand();
            //command.TransactionName = "test";

            _transactionRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                        .ReturnsAsync(new Transaction() { /*TODO:propertyler buraya yazılacak TransactionId = 1, TransactionName = "deneme"*/ });

            _transactionRepository.Setup(x => x.Update(It.IsAny<Transaction>())).Returns(new Transaction());

            var handler = new UpdateTransactionCommandHandler(_transactionRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            _transactionRepository.Verify(x => x.SaveChangesAsync());
            x.Success.Should().BeTrue();
            x.Message.Should().Be(Messages.Updated);
        }

        [Test]
        public async Task Transaction_DeleteCommand_Success()
        {
            //Arrange
            var command = new DeleteTransactionCommand();

            _transactionRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                        .ReturnsAsync(new Transaction() { /*TODO:propertyler buraya yazılacak TransactionId = 1, TransactionName = "deneme"*/});

            _transactionRepository.Setup(x => x.Delete(It.IsAny<Transaction>()));

            var handler = new DeleteTransactionCommandHandler(_transactionRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            _transactionRepository.Verify(x => x.SaveChangesAsync());
            x.Success.Should().BeTrue();
            x.Message.Should().Be(Messages.Deleted);
        }
    }
}

