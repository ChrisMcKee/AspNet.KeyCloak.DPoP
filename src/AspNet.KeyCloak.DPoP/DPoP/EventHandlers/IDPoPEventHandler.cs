using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AspNetCore.Authentication.Api.DPoP.EventHandlers;

public interface IDPoPEventHandler<T>
{
    /// <summary>
    ///     Handles the event with the provided context.
    /// </summary>
    /// <param name="context">Context based on the event, like <see cref="MessageReceivedContext" /></param>
    /// <returns></returns>
    Task Handle(T context);
}
