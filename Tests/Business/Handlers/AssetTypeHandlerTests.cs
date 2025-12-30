
using Business.Handlers.AssetTypes.Queries;
using DataAccess.Abstract;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static Business.Handlers.AssetTypes.Queries.GetAssetTypeQuery;
using Entities.Concrete;
using static Business.Handlers.AssetTypes.Queries.GetAssetTypesQuery;
using static Business.Handlers.AssetTypes.Commands.CreateAssetTypeCommand;
using Business.Handlers.AssetTypes.Commands;
using Business.Constants;
using static Business.Handlers.AssetTypes.Commands.UpdateAssetTypeCommand;
using static Business.Handlers.AssetTypes.Commands.DeleteAssetTypeCommand;
using MediatR;
using System.Linq;
using FluentAssertions;
using Core.Entities.Concrete;


namespace Tests.Business.HandlersTest
{
    [TestFixture]
    public class AssetTypeHandlerTests
    {
        Mock<IAssetTypeRepository> _assetTypeRepository;
        Mock<IMediator> _mediator;
        [SetUp]
        public void Setup()
        {
            _assetTypeRepository = new Mock<IAssetTypeRepository>();
            _mediator = new Mock<IMediator>();
        }

        [Test]
        public async Task AssetType_GetQuery_Success()
        {
            //Arrange
            var query = new GetAssetTypeQuery();

            _assetTypeRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<AssetType, bool>>>())).ReturnsAsync(new AssetType()
//propertyler buraya yazılacak
//{																		
//AssetTypeId = 1,
//AssetTypeName = "Test"
//}
);

            var handler = new GetAssetTypeQueryHandler(_assetTypeRepository.Object, _mediator.Object);

            //Act
            var x = await handler.Handle(query, new System.Threading.CancellationToken());

            //Asset
            x.Success.Should().BeTrue();
            //x.Data.AssetTypeId.Should().Be(1);

        }

        [Test]
        public async Task AssetType_GetQueries_Success()
        {
            //Arrange
            var query = new GetAssetTypesQuery();

            _assetTypeRepository.Setup(x => x.GetListAsync(It.IsAny<Expression<Func<AssetType, bool>>>()))
                        .ReturnsAsync(new List<AssetType> { new AssetType() { /*TODO:propertyler buraya yazılacak AssetTypeId = 1, AssetTypeName = "test"*/ } });

            var handler = new GetAssetTypesQueryHandler(_assetTypeRepository.Object, _mediator.Object);

            //Act
            var x = await handler.Handle(query, new System.Threading.CancellationToken());

            //Asset
            x.Success.Should().BeTrue();
            ((List<AssetType>)x.Data).Count.Should().BeGreaterThan(1);

        }

        [Test]
        //public async Task AssetType_CreateCommand_Success()
        //{
        //    AssetType rt = null;
        //    //Arrange
        //    var command = new CreateAssetTypeCommand();
        //    //propertyler buraya yazılacak
        //    //command.AssetTypeName = "deneme";

        //    _assetTypeRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<AssetType, bool>>>()))
        //                .ReturnsAsync(rt);

        //    _assetTypeRepository.Setup(x => x.Add(It.IsAny<AssetType>())).Returns(new AssetType());

        //    var handler = new CreateAssetTypeCommandHandler(_assetTypeRepository.Object, _mediator.Object);
        //    var x = await handler.Handle(command, new System.Threading.CancellationToken());

        //    _assetTypeRepository.Verify(x => x.SaveChangesAsync());
        //    x.Success.Should().BeTrue();
        //    x.Message.Should().Be(Messages.Added);
        //}

        //[Test]
        //public async Task AssetType_CreateCommand_NameAlreadyExist()
        //{
        //    //Arrange
        //    var command = new CreateAssetTypeCommand();
        //    //propertyler buraya yazılacak 
        //    //command.AssetTypeName = "test";

        //    _assetTypeRepository.Setup(x => x.Query())
        //                                   .Returns(new List<AssetType> { new AssetType() { /*TODO:propertyler buraya yazılacak AssetTypeId = 1, AssetTypeName = "test"*/ } }.AsQueryable());

        //    _assetTypeRepository.Setup(x => x.Add(It.IsAny<AssetType>())).Returns(new AssetType());

        //    var handler = new CreateAssetTypeCommandHandler(_assetTypeRepository.Object, _mediator.Object);
        //    var x = await handler.Handle(command, new System.Threading.CancellationToken());

        //    x.Success.Should().BeFalse();
        //    x.Message.Should().Be(Messages.NameAlreadyExist);
        //}

        //[Test]
        public async Task AssetType_UpdateCommand_Success()
        {
            //Arrange
            var command = new UpdateAssetTypeCommand();
            //command.AssetTypeName = "test";

            _assetTypeRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<AssetType, bool>>>()))
                        .ReturnsAsync(new AssetType() { /*TODO:propertyler buraya yazılacak AssetTypeId = 1, AssetTypeName = "deneme"*/ });

            _assetTypeRepository.Setup(x => x.Update(It.IsAny<AssetType>())).Returns(new AssetType());

            var handler = new UpdateAssetTypeCommandHandler(_assetTypeRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            _assetTypeRepository.Verify(x => x.SaveChangesAsync());
            x.Success.Should().BeTrue();
            x.Message.Should().Be(Messages.Updated);
        }

        [Test]
        public async Task AssetType_DeleteCommand_Success()
        {
            ////Arrange
            //var command = new DeleteAssetTypeCommand();

            //_assetTypeRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<AssetType, bool>>>()))
            //            .ReturnsAsync(new AssetType() { /*TODO:propertyler buraya yazılacak AssetTypeId = 1, AssetTypeName = "deneme"*/});

            //_assetTypeRepository.Setup(x => x.Delete(It.IsAny<AssetType>()));

            //var handler = new DeleteAssetTypeCommandHandler(_assetTypeRepository.Object, _mediator.Object);
            //var x = await handler.Handle(command, new System.Threading.CancellationToken());

            //_assetTypeRepository.Verify(x => x.SaveChangesAsync());
            //x.Success.Should().BeTrue();
            //x.Message.Should().Be(Messages.Deleted);
        }
    }
}

