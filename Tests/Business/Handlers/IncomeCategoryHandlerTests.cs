
using Business.Handlers.IncomeCategories.Queries;
using DataAccess.Abstract;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static Business.Handlers.IncomeCategories.Queries.GetIncomeCategoryQuery;
using Entities.Concrete;
using static Business.Handlers.IncomeCategories.Queries.GetIncomeCategoriesQuery;
using static Business.Handlers.IncomeCategories.Commands.CreateIncomeCategoryCommand;
using Business.Handlers.IncomeCategories.Commands;
using Business.Constants;
using static Business.Handlers.IncomeCategories.Commands.UpdateIncomeCategoryCommand;
using static Business.Handlers.IncomeCategories.Commands.DeleteIncomeCategoryCommand;
using MediatR;
using System.Linq;
using FluentAssertions;
using Core.Entities.Concrete;


namespace Tests.Business.HandlersTest
{
    [TestFixture]
    public class IncomeCategoryHandlerTests
    {
        Mock<IIncomeCategoryRepository> _incomeCategoryRepository;
        Mock<IMediator> _mediator;
        [SetUp]
        public void Setup()
        {
            _incomeCategoryRepository = new Mock<IIncomeCategoryRepository>();
            _mediator = new Mock<IMediator>();
        }

        [Test]
        public async Task IncomeCategory_GetQuery_Success()
        {
            //Arrange
            var query = new GetIncomeCategoryQuery();

            _incomeCategoryRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<IncomeCategory, bool>>>())).ReturnsAsync(new IncomeCategory()
//propertyler buraya yazılacak
//{																		
//IncomeCategoryId = 1,
//IncomeCategoryName = "Test"
//}
);

            var handler = new GetIncomeCategoryQueryHandler(_incomeCategoryRepository.Object, _mediator.Object);

            //Act
            var x = await handler.Handle(query, new System.Threading.CancellationToken());

            //Asset
            x.Success.Should().BeTrue();
            //x.Data.IncomeCategoryId.Should().Be(1);

        }

        [Test]
        public async Task IncomeCategory_GetQueries_Success()
        {
            //Arrange
            var query = new GetIncomeCategoriesQuery();

            _incomeCategoryRepository.Setup(x => x.GetListAsync(It.IsAny<Expression<Func<IncomeCategory, bool>>>()))
                        .ReturnsAsync(new List<IncomeCategory> { new IncomeCategory() { /*TODO:propertyler buraya yazılacak IncomeCategoryId = 1, IncomeCategoryName = "test"*/ } });

            var handler = new GetIncomeCategoriesQueryHandler(_incomeCategoryRepository.Object, _mediator.Object);

            //Act
            var x = await handler.Handle(query, new System.Threading.CancellationToken());

            //Asset
            x.Success.Should().BeTrue();
            ((List<IncomeCategory>)x.Data).Count.Should().BeGreaterThan(1);

        }

        [Test]
        public async Task IncomeCategory_CreateCommand_Success()
        {
            IncomeCategory rt = null;
            //Arrange
            var command = new CreateIncomeCategoryCommand();
            //propertyler buraya yazılacak
            //command.IncomeCategoryName = "deneme";

            _incomeCategoryRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<IncomeCategory, bool>>>()))
                        .ReturnsAsync(rt);

            _incomeCategoryRepository.Setup(x => x.Add(It.IsAny<IncomeCategory>())).Returns(new IncomeCategory());

            var handler = new CreateIncomeCategoryCommandHandler(_incomeCategoryRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            _incomeCategoryRepository.Verify(x => x.SaveChangesAsync());
            x.Success.Should().BeTrue();
            x.Message.Should().Be(Messages.Added);
        }

        [Test]
        public async Task IncomeCategory_CreateCommand_NameAlreadyExist()
        {
            //Arrange
            var command = new CreateIncomeCategoryCommand();
            //propertyler buraya yazılacak 
            //command.IncomeCategoryName = "test";

            _incomeCategoryRepository.Setup(x => x.Query())
                                           .Returns(new List<IncomeCategory> { new IncomeCategory() { /*TODO:propertyler buraya yazılacak IncomeCategoryId = 1, IncomeCategoryName = "test"*/ } }.AsQueryable());

            _incomeCategoryRepository.Setup(x => x.Add(It.IsAny<IncomeCategory>())).Returns(new IncomeCategory());

            var handler = new CreateIncomeCategoryCommandHandler(_incomeCategoryRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            x.Success.Should().BeFalse();
            x.Message.Should().Be(Messages.NameAlreadyExist);
        }

        [Test]
        public async Task IncomeCategory_UpdateCommand_Success()
        {
            //Arrange
            var command = new UpdateIncomeCategoryCommand();
            //command.IncomeCategoryName = "test";

            _incomeCategoryRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<IncomeCategory, bool>>>()))
                        .ReturnsAsync(new IncomeCategory() { /*TODO:propertyler buraya yazılacak IncomeCategoryId = 1, IncomeCategoryName = "deneme"*/ });

            _incomeCategoryRepository.Setup(x => x.Update(It.IsAny<IncomeCategory>())).Returns(new IncomeCategory());

            var handler = new UpdateIncomeCategoryCommandHandler(_incomeCategoryRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            _incomeCategoryRepository.Verify(x => x.SaveChangesAsync());
            x.Success.Should().BeTrue();
            x.Message.Should().Be(Messages.Updated);
        }

        [Test]
        public async Task IncomeCategory_DeleteCommand_Success()
        {
            ////Arrange
            //var command = new DeleteIncomeCategoryCommand();

            //_incomeCategoryRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<IncomeCategory, bool>>>()))
            //            .ReturnsAsync(new IncomeCategory() { /*TODO:propertyler buraya yazılacak IncomeCategoryId = 1, IncomeCategoryName = "deneme"*/});

            //_incomeCategoryRepository.Setup(x => x.Delete(It.IsAny<IncomeCategory>()));

            //var handler = new DeleteIncomeCategoryCommandHandler(_incomeCategoryRepository.Object, _mediator.Object);
            //var x = await handler.Handle(command, new System.Threading.CancellationToken());

            //_incomeCategoryRepository.Verify(x => x.SaveChangesAsync());
            //x.Success.Should().BeTrue();
            //x.Message.Should().Be(Messages.Deleted);
        }
    }
}

