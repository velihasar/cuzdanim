using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Business.Constants;
using Business.Handlers.Authorizations.Commands;
using Business.Handlers.Authorizations.Queries;
using Business.Services.Authentication;
using Core.CrossCuttingConcerns.Caching;
using Core.Entities.Concrete;
using Core.Utilities.Security.Hashing;
using Core.Utilities.Security.Jwt;
using Core.Utilities.Security.Turnstile;
using DataAccess.Abstract;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using Tests.Helpers;
using static Business.Handlers.Authorizations.Commands.ForgotPasswordCommand;
using static Business.Handlers.Authorizations.Queries.LoginUserQuery;
using static Business.Handlers.Authorizations.Queries.LoginWithRefreshTokenQuery;
using static Business.Handlers.Authorizations.Commands.RegisterUserCommand;

namespace Tests.Business.Handlers
{
    

    [TestFixture]
    public class AuthorizationsTests
    {
        private Mock<IUserRepository> _userRepository;
        private Mock<ITokenHelper> _tokenHelper;
        private Mock<IMediator> _mediator;
        private Mock<ICacheManager> _cacheManager;
        private Mock<IUserGroupRepository> _userGroupRepository;
        private Mock<IGroupRepository> _groupRepository;
        private Mock<TurnstileHelper> _turnstileHelper;
        private Mock<IHttpContextAccessor> _httpContextAccessor;

        private LoginUserQueryHandler _loginUserQueryHandler;
        private LoginUserQuery _loginUserQuery;
        private RegisterUserCommandHandler _registerUserCommandHandler;
        private RegisterUserCommand _command;
        private ForgotPasswordCommandHandler _forgotPasswordCommandHandler;
        private ForgotPasswordCommand _forgotPasswordCommand;

        [SetUp]
        //public void Setup()
        //{
        //    _userRepository = new Mock<IUserRepository>();
        //    _tokenHelper = new Mock<ITokenHelper>();
        //    _mediator = new Mock<IMediator>();
        //    _cacheManager = new Mock<ICacheManager>();
        //    _userGroupRepository = new Mock<IUserGroupRepository>();
        //    _groupRepository = new Mock<IGroupRepository>();
        //    _turnstileHelper = new Mock<TurnstileHelper>(Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(), Mock.Of<System.Net.Http.HttpClient>());
        //    _httpContextAccessor = new Mock<IHttpContextAccessor>();

        //    _loginUserQueryHandler = new LoginUserQueryHandler(_userRepository.Object, _tokenHelper.Object, _mediator.Object, _cacheManager.Object);
        //    _registerUserCommandHandler = new RegisterUserCommandHandler(_userRepository.Object, _userGroupRepository.Object, _groupRepository.Object, _turnstileHelper.Object, _httpContextAccessor.Object);
        //    //_forgotPasswordCommandHandler = new ForgotPasswordCommandHandler(_userRepository.Object);
        //}

        [Test]
        public async Task Handler_Login()
        {
            var user = DataHelper.GetUser("test");
            HashingHelper.CreatePasswordHash("123456", out var passwordSalt, out var passwordHash);
            user.PasswordSalt = passwordSalt;
            user.PasswordHash = passwordHash;
            _userRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .Returns(() => Task.FromResult(user));


            _tokenHelper.Setup(x => x.CreateToken<DArchToken>(It.IsAny<User>())).Returns(new DArchToken()
            {
                Token = "TestToken",
                Claims = new List<string>(),
                Expiration = DateTime.Now.AddHours(1)
            });

            _userRepository.Setup(x => x.GetClaims(It.IsAny<int>()))
                .Returns(new List<OperationClaim>() { new OperationClaim() { Id = 1, Name = "test" } });
            _loginUserQuery = new LoginUserQuery
            {
                UserName = user.UserName,
                Password = "123456"
            };

            var result = await _loginUserQueryHandler.Handle(_loginUserQuery, new CancellationToken());

            result.Success.Should().BeTrue();
        }

        [Test]
        public async Task Handler_LoginWithRefreshTokenCommand_Success()
        {
            var command = new LoginWithRefreshTokenQuery
            {
                RefreshToken = Guid.NewGuid().ToString()
            };

            _userRepository.Setup(x => x.GetByRefreshToken(It.IsAny<string>()))
                .ReturnsAsync(DataHelper.GetUser("test"));
            _userRepository.Setup(x => x.GetClaims(It.IsAny<int>()))
                .Returns(new List<OperationClaim> { new OperationClaim { } });
            _userRepository.Setup(x => x.Update(It.IsAny<User>())).Returns(new User());
            _tokenHelper.Setup(x => x.CreateToken<AccessToken>(It.IsAny<User>())).Returns(new AccessToken());

            var handler =
                new LoginWithRefreshTokenQueryHandler(_userRepository.Object, _tokenHelper.Object, _cacheManager.Object);
            var x = await handler.Handle(command, new CancellationToken());

            _userRepository.Verify(x => x.GetByRefreshToken(It.IsAny<string>()), Times.Once);
            _userRepository.Verify(x => x.GetClaims(It.IsAny<int>()), Times.Once);
            _userRepository.Verify(x => x.Update(It.IsAny<User>()), Times.Once);
            _userRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
            _tokenHelper.Verify(x => x.CreateToken<AccessToken>(It.IsAny<User>()), Times.Once);
            x.Success.Should().BeTrue();
            x.Message.Should().Be(Messages.SuccessfulLogin);
        }

        [Test]
        public async Task User_LoginWithRefreshTokenCommand_UserNotFound()
        {
            var command = new LoginWithRefreshTokenQuery
            {
                RefreshToken = Guid.NewGuid().ToString()
            };

            User rt = null;

            _userRepository.Setup(x => x.GetByRefreshToken(It.IsAny<string>())).ReturnsAsync(rt);

            var handler =
                new LoginWithRefreshTokenQueryHandler(_userRepository.Object, _tokenHelper.Object, _cacheManager.Object);
            var x = await handler.Handle(command, new CancellationToken());

            _userRepository.Verify(x => x.GetByRefreshToken(It.IsAny<string>()), Times.Once);
            _userRepository.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
            _userRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
            _tokenHelper.Verify(x => x.CreateToken<AccessToken>(It.IsAny<User>()), Times.Never);
            x.Success.Should().BeFalse();
            x.Message.Should().Be(Messages.UserNotFound);
        }

        [Test]
        public async Task Handler_Register()
        {
            //var registerUser = new User { UserName = "testuser", Email = "test@test.com", FullName = "test test" };
            //_command = new RegisterUserCommand
            //{
            //    UserName = registerUser.UserName,
            //    Email = registerUser.Email,
            //    FullName = registerUser.FullName,
            //    Password = "123456",
            //    TurnstileToken = "test-turnstile-token"
            //};
            
            //// Mock Turnstile verification
            //_turnstileHelper.Setup(x => x.VerifyTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            //    .ReturnsAsync(true);
            
            //// Mock HttpContext
            //var httpContext = new Mock<HttpContext>();
            //var connection = new Mock<ConnectionInfo>();
            //connection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));
            //httpContext.Setup(x => x.Connection).Returns(connection.Object);
            //_httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);
            
            //// Mock repository setups
            //_userRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
            //    .ReturnsAsync((User)null); // User doesn't exist
            //_userRepository.Setup(x => x.Add(It.IsAny<User>()));
            //_userRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
            
            //_groupRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<Group, bool>>>()))
            //    .ReturnsAsync((Group)null); // Default Group doesn't exist (optional)
            //_userGroupRepository.Setup(x => x.Add(It.IsAny<UserGroup>()));
            //_userGroupRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
            
            //var result = await _registerUserCommandHandler.Handle(_command, CancellationToken.None);

            //result.Message.Should().Be(Messages.Added);
        }

        [Test]
        public async Task Handler_ForgotPassword()
        {
            //var user = DataHelper.GetUser("test");
            //_userRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
            //    .Returns(() => Task.FromResult(user));
            //_forgotPasswordCommand = new ForgotPasswordCommand
            //{
            //    Email = user.Email,
            //    UserId = Convert.ToString(user.UserId)
            //};
            //var result =
            //    await _forgotPasswordCommandHandler.Handle(_forgotPasswordCommand, new CancellationToken());
            //result.Success.Should().BeTrue();
        }
    }
}