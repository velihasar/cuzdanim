
using Business.Handlers.Assets.Queries;
using DataAccess.Abstract;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static Business.Handlers.Assets.Queries.GetAssetQuery;
using Entities.Concrete;
using static Business.Handlers.Assets.Queries.GetAssetsQuery;
using static Business.Handlers.Assets.Commands.CreateAssetCommand;
using Business.Handlers.Assets.Commands;
using Business.Constants;
using static Business.Handlers.Assets.Commands.UpdateAssetCommand;
using static Business.Handlers.Assets.Commands.DeleteAssetCommand;
using MediatR;
using System.Linq;
using FluentAssertions;
using Core.Entities.Concrete;


namespace Tests.Business.HandlersTest
{
    [TestFixture]
    public class AssetHandlerTests
    {
        Mock<IAssetRepository> _assetRepository;
        Mock<IMediator> _mediator;
        [SetUp]
        public void Setup()
        {
            _assetRepository = new Mock<IAssetRepository>();
            _mediator = new Mock<IMediator>();
        }

        [Test]
        public async Task Asset_GetQuery_Success()
        {
            //Arrange
            var query = new GetAssetQuery();

            _assetRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<Asset, bool>>>())).ReturnsAsync(new Asset()
//propertyler buraya yazılacak
//{																		
//AssetId = 1,
//AssetName = "Test"
//}
);

            var handler = new GetAssetQueryHandler(_assetRepository.Object, _mediator.Object);

            //Act
            var x = await handler.Handle(query, new System.Threading.CancellationToken());

            //Asset
            x.Success.Should().BeTrue();
            //x.Data.AssetId.Should().Be(1);

        }

        [Test]
        public async Task Asset_GetQueries_Success()
        {
            //Arrange
            var query = new GetAssetsQuery();

            _assetRepository.Setup(x => x.GetListAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                        .ReturnsAsync(new List<Asset> { new Asset() { /*TODO:propertyler buraya yazılacak AssetId = 1, AssetName = "test"*/ } });

            var handler = new GetAssetsQueryHandler(_assetRepository.Object, _mediator.Object);

            //Act
            var x = await handler.Handle(query, new System.Threading.CancellationToken());

            //Asset
            x.Success.Should().BeTrue();
            ((List<Asset>)x.Data).Count.Should().BeGreaterThan(1);

        }

        [Test]
        public async Task Asset_CreateCommand_Success()
        {
            Asset rt = null;
            //Arrange
            var command = new CreateAssetCommand();
            //propertyler buraya yazılacak
            //command.AssetName = "deneme";

            _assetRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                        .ReturnsAsync(rt);

            _assetRepository.Setup(x => x.Add(It.IsAny<Asset>())).Returns(new Asset());

            var handler = new CreateAssetCommandHandler(_assetRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            _assetRepository.Verify(x => x.SaveChangesAsync());
            x.Success.Should().BeTrue();
            x.Message.Should().Be(Messages.Added);
        }

        [Test]
        public async Task Asset_CreateCommand_NameAlreadyExist()
        {
            //Arrange
            var command = new CreateAssetCommand();
            //propertyler buraya yazılacak 
            //command.AssetName = "test";

            _assetRepository.Setup(x => x.Query())
                                           .Returns(new List<Asset> { new Asset() { /*TODO:propertyler buraya yazılacak AssetId = 1, AssetName = "test"*/ } }.AsQueryable());

            _assetRepository.Setup(x => x.Add(It.IsAny<Asset>())).Returns(new Asset());

            var handler = new CreateAssetCommandHandler(_assetRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            x.Success.Should().BeFalse();
            x.Message.Should().Be(Messages.NameAlreadyExist);
        }

        [Test]
        public async Task Asset_UpdateCommand_Success()
        {
            //Arrange
            var command = new UpdateAssetCommand();
            //command.AssetName = "test";

            _assetRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                        .ReturnsAsync(new Asset() { /*TODO:propertyler buraya yazılacak AssetId = 1, AssetName = "deneme"*/ });

            _assetRepository.Setup(x => x.Update(It.IsAny<Asset>())).Returns(new Asset());

            var handler = new UpdateAssetCommandHandler(_assetRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            _assetRepository.Verify(x => x.SaveChangesAsync());
            x.Success.Should().BeTrue();
            x.Message.Should().Be(Messages.Updated);
        }

        [Test]
        public async Task Asset_DeleteCommand_Success()
        {
            //Arrange
            var command = new DeleteAssetCommand();

            _assetRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                        .ReturnsAsync(new Asset() { /*TODO:propertyler buraya yazılacak AssetId = 1, AssetName = "deneme"*/});

            _assetRepository.Setup(x => x.Delete(It.IsAny<Asset>()));

            var handler = new DeleteAssetCommandHandler(_assetRepository.Object, _mediator.Object);
            var x = await handler.Handle(command, new System.Threading.CancellationToken());

            _assetRepository.Verify(x => x.SaveChangesAsync());
            x.Success.Should().BeTrue();
            x.Message.Should().Be(Messages.Deleted);
        }
    }
}

