using AspNet.KeyCloak.DPoP.DPoP;
using AspNet.KeyCloak.DPoP.DPoP.EventHandlers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests;

public class DPoPEventHandlersTest
{
    private readonly DPoPEventHandlers _service;

    public DPoPEventHandlersTest()
    {
        _service = TestUtilities.CreateDPoPEventHandlers();
    }

    [Fact]
    public void HandleOnMessageReceived_Should_Return_Function_That_Executes_MessageReceivedHandler()
    {
        Func<MessageReceivedContext, Task> handler = _service.HandleOnMessageReceived();

        handler.Should().NotBeNull();
        handler.Should().BeOfType<Func<MessageReceivedContext, Task>>();
    }

    [Fact]
    public async Task HandleOnMessageReceived_Should_Use_Registered_MessageReceivedHandler_From_DI()
    {
        // Create a custom handler to verify it's being used
        var customHandlerCalled = false;
        var customHandler = A.Fake<MessageReceivedHandler>();
        A.CallTo(() => customHandler.HandleDisabledMode(A<MessageReceivedContext>._))
            .Invokes(() => customHandlerCalled = true)
            .Returns(Task.CompletedTask);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(customHandler);
        serviceCollection.AddSingleton(new DPoPOptions { Mode = DPoPModes.Disabled });
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var context = new MessageReceivedContext(httpContext,
            new AuthenticationScheme("test", null, typeof(JwtBearerHandler)), new JwtBearerOptions());

        Func<MessageReceivedContext, Task> handler = _service.HandleOnMessageReceived();
        await handler(context);

        customHandlerCalled.Should().BeTrue("the registered handler should be used from DI");
    }

    [Fact]
    public async Task HandleOnMessageReceived_Should_Use_Default_MessageReceivedHandler_When_Not_Registered()
    {
        IServiceProvider serviceProvider = TestUtilities.CreateServiceProviderWithDPoPOptions();
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var context = new MessageReceivedContext(httpContext,
            new AuthenticationScheme("test", null, typeof(JwtBearerHandler)), new JwtBearerOptions());

        Func<MessageReceivedContext, Task> handler = _service.HandleOnMessageReceived();
        Func<Task> act = async () => await handler(context);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void HandleOnTokenValidated_Should_Return_Function_That_Executes_TokenValidationHandler()
    {
        Func<TokenValidatedContext, Task> handler = _service.HandleOnTokenValidated();

        handler.Should().NotBeNull();
        handler.Should().BeOfType<Func<TokenValidatedContext, Task>>();
    }

    [Fact]
    public async Task HandleOnTokenValidated_Should_Use_Registered_TokenValidationHandler_From_DI()
    {
        // Create a custom handler to verify it's being used
        var customHandlerCalled = false;
        var fakeValidationService = A.Fake<IDPoPProofValidationService>();
        var customHandler = A.Fake<TokenValidationHandler>(o => o.WithArgumentsForConstructor(new object[] { fakeValidationService }));
        A.CallTo(() => customHandler.HandleDisabledMode(A<TokenValidatedContext>._))
            .Invokes(() => customHandlerCalled = true)
            .Returns(Task.CompletedTask);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<TokenValidationHandler>(customHandler);
        serviceCollection.AddSingleton(new DPoPOptions { Mode = DPoPModes.Disabled });
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var context = new TokenValidatedContext(httpContext,
            new AuthenticationScheme("test", null, typeof(JwtBearerHandler)), new JwtBearerOptions());

        Func<TokenValidatedContext, Task> handler = _service.HandleOnTokenValidated();
        await handler(context);

        customHandlerCalled.Should().BeTrue("the registered handler should be used from DI");
    }

    [Fact]
    public async Task
        HandleOnTokenValidated_Should_Create_TokenValidationHandler_With_ValidationService_When_Handler_Not_Registered()
    {
        var fakeValidationService = A.Fake<IDPoPProofValidationService>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(fakeValidationService);
        serviceCollection.AddSingleton(new DPoPOptions { Mode = DPoPModes.Disabled });
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var context = new TokenValidatedContext(httpContext,
            new AuthenticationScheme("test", null, typeof(JwtBearerHandler)), new JwtBearerOptions());

        Func<TokenValidatedContext, Task> handler = _service.HandleOnTokenValidated();
        Func<Task> act = async () => await handler(context);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void HandleOnChallenge_Should_Return_Function_That_Executes_ChallengeHandler()
    {
        Func<JwtBearerChallengeContext, Task> handler = _service.HandleOnChallenge();

        handler.Should().NotBeNull();
        handler.Should().BeOfType<Func<JwtBearerChallengeContext, Task>>();
    }

    [Fact]
    public async Task HandleOnChallenge_Should_Use_Registered_ChallengeHandler_From_DI()
    {
        // Create a custom handler to verify it's being used
        var customHandlerCalled = false;
        var customHandler = A.Fake<ChallengeHandler>();
        A.CallTo(() => customHandler.HandleDisabledMode(A<JwtBearerChallengeContext>._))
            .Invokes(() => customHandlerCalled = true)
            .Returns(Task.CompletedTask);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(customHandler);
        serviceCollection.AddSingleton(new DPoPOptions { Mode = DPoPModes.Disabled });
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var context = new JwtBearerChallengeContext(httpContext,
            new AuthenticationScheme("test", null, typeof(JwtBearerHandler)), new JwtBearerOptions(),
            new AuthenticationProperties());

        Func<JwtBearerChallengeContext, Task> handler = _service.HandleOnChallenge();
        await handler(context);

        customHandlerCalled.Should().BeTrue("the registered handler should be used from DI");
    }

    [Fact]
    public async Task HandleOnChallenge_Should_Use_Default_ChallengeHandler_When_Not_Registered()
    {
        IServiceProvider serviceProvider = TestUtilities.CreateServiceProviderWithDPoPOptions();
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var context = new JwtBearerChallengeContext(httpContext,
            new AuthenticationScheme("test", null, typeof(JwtBearerHandler)), new JwtBearerOptions(),
            new AuthenticationProperties());

        Func<JwtBearerChallengeContext, Task> handler = _service.HandleOnChallenge();
        Func<Task> act = async () => await handler(context);

        await act.Should().NotThrowAsync();
    }
}
