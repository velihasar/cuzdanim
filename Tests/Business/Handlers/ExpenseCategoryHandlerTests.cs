
using Business.Handlers.ExpenseCategories.Queries;
using DataAccess.Abstract;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static Business.Handlers.ExpenseCategories.Queries.GetExpenseCategoryQuery;
using Entities.Concrete;
using static Business.Handlers.ExpenseCategories.Queries.GetExpenseCategoriesQuery;
using static Business.Handlers.ExpenseCategories.Commands.CreateExpenseCategoryCommand;
using Business.Handlers.ExpenseCategories.Commands;
using Business.Constants;
using static Business.Handlers.ExpenseCategories.Commands.UpdateExpenseCategoryCommand;
using static Business.Handlers.ExpenseCategories.Commands.DeleteExpenseCategoryCommand;
using MediatR;
using System.Linq;
using FluentAssertions;
using Core.Entities.Concrete;


namespace Tests.Business.HandlersTest
{
    [TestFixture]
    public class ExpenseCategoryHandlerTests
    {
        Mock<IExpenseCategoryRepository> _expenseCategoryRepository;
        Mock<IMediator> _mediator;
        [SetUp]
        public void Setup()
        {
            _expenseCategoryRepository = new Mock<IExpenseCategoryRepository>();
            _mediator = new Mock<IMediator>();
        }

        [Test]
        public async Task ExpenseCategory_GetQuery_Success()
        {
            //Arrange
            var query = new GetExpenseCategoryQuery();

            _expenseCategoryRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<ExpenseCategory, bool>>>())).ReturnsAsync(new ExpenseCategory()
//propertyler buraya yazılacak
//{																		
//ExpenseCategoryId = 1,
//ExpenseCategoryName = "Test"
//}
);

            var handler = new GetExpenseCategoryQueryHandler(_expenseCategoryRepository.Object, _mediator.Object);

            //Act
            var x = await handler.Handle(query, new System.Threading.CancellationToken());

            //Asset
            x.Success.Should().BeTrue();
            //x.Data.ExpenseCategoryId.Should().Be(1);

        }

        [Test]
        public async Task ExpenseCategory_GetQueries_Success()
        {
            //Arrange
            var query = new GetExpenseCategoriesQuery();

            _expenseCategoryRepository.Setup(x => x.GetListAsync(It.IsAny<Expression<Func<ExpenseCategory, bool>>>()))
                        .ReturnsAsync(new List<ExpenseCategory> { new ExpenseCategory() { /*TODO:propertyler buraya yazılacak ExpenseCategoryId = 1, ExpenseCategoryName = "test"*/ } });

            var handler = new GetExpenseCategoriesQueryHandler(_expenseCategoryRepository.Object, _mediator.Object);

            //Act
            var x = await handler.Handle(query, new System.Threading.CancellationToken());

            //Asset
            x.Success.Should().BeTrue();
            ((List<ExpenseCategory>)x.Data).Count.Should().BeGreaterThan(1);

        }

        [Test]
        public async Task ExpenseCategory_CreateCommand_Success()
        {
            ExpenseCategory rt = null;
            //Arrange
            var command = new CreateExpenseCategoryCommand();
            //propertyler buraya yazılacak
            //command.ExpenseCategoryName = "deneme";

            _expenseCategoryRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<ExpenseCategory, bool>>>()))
                        .ReturnsAsync(rt);

            _expenseCategoryRepository.Setup(x => x.Add(It.IsAny<ExpenseCategory>())).Returns(new ExpenseCategory());

            var handler = new CreateExpenseCategoryCommandHandler(_expenseCategoryRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            _expenseCategoryRepository.Verify(x => x.SaveChangesAsync());
            x.Success.Should().BeTrue();
            x.Message.Should().Be(Messages.Added);
        }

        [Test]
        public async Task ExpenseCategory_CreateCommand_NameAlreadyExist()
        {
            //Arrange
            var command = new CreateExpenseCategoryCommand();
            //propertyler buraya yazılacak 
            //command.ExpenseCategoryName = "test";

            _expenseCategoryRepository.Setup(x => x.Query())
                                           .Returns(new List<ExpenseCategory> { new ExpenseCategory() { /*TODO:propertyler buraya yazılacak ExpenseCategoryId = 1, ExpenseCategoryName = "test"*/ } }.AsQueryable());

            _expenseCategoryRepository.Setup(x => x.Add(It.IsAny<ExpenseCategory>())).Returns(new ExpenseCategory());

            var handler = new CreateExpenseCategoryCommandHandler(_expenseCategoryRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            x.Success.Should().BeFalse();
            x.Message.Should().Be(Messages.NameAlreadyExist);
        }

        [Test]
        public async Task ExpenseCategory_UpdateCommand_Success()
        {
            //Arrange
            var command = new UpdateExpenseCategoryCommand();
            //command.ExpenseCategoryName = "test";

            _expenseCategoryRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<ExpenseCategory, bool>>>()))
                        .ReturnsAsync(new ExpenseCategory() { /*TODO:propertyler buraya yazılacak ExpenseCategoryId = 1, ExpenseCategoryName = "deneme"*/ });

            _expenseCategoryRepository.Setup(x => x.Update(It.IsAny<ExpenseCategory>())).Returns(new ExpenseCategory());

            var handler = new UpdateExpenseCategoryCommandHandler(_expenseCategoryRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            _expenseCategoryRepository.Verify(x => x.SaveChangesAsync());
            x.Success.Should().BeTrue();
            x.Message.Should().Be(Messages.Updated);
        }

        [Test]
        public async Task ExpenseCategory_DeleteCommand_Success()
        {
            ////Arrange
            //var command = new DeleteExpenseCategoryCommand();

            //_expenseCategoryRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<ExpenseCategory, bool>>>()))
            //            .ReturnsAsync(new ExpenseCategory() { /*TODO:propertyler buraya yazılacak ExpenseCategoryId = 1, ExpenseCategoryName = "deneme"*/});

            //_expenseCategoryRepository.Setup(x => x.Delete(It.IsAny<ExpenseCategory>()));

            //var handler = new DeleteExpenseCategoryCommandHandler(_expenseCategoryRepository.Object, _mediator.Object);
            //var x = await handler.Handle(command, new System.Threading.CancellationToken());

            //_expenseCategoryRepository.Verify(x => x.SaveChangesAsync());
            //x.Success.Should().BeTrue();
            //x.Message.Should().Be(Messages.Deleted);
        }
    }
}

