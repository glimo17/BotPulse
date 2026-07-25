using BotPulse.Api.Middleware;
using BotPulse.Core.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace BotPulse.UnitTests.Infrastructure;

public sealed class ErrorHandlerMiddlewareTests
{
    private static ErrorHandlerMiddleware CreateSut(int thrownStatusCode = 0, Exception? ex = null)
    {
        RequestDelegate next = ex is not null
            ? _ => Task.FromException(ex)
            : ctx => { ctx.Response.StatusCode = thrownStatusCode; return Task.CompletedTask; };

        return new ErrorHandlerMiddleware(next, NullLogger<ErrorHandlerMiddleware>.Instance);
    }

    [Fact]
    public async Task ValidationException_ShouldReturn400()
    {
        var sut = CreateSut(ex: new ValidationException([new ValidationError("field", "required")]));
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        await sut.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task EntityNotFoundException_ShouldReturn404()
    {
        var sut = CreateSut(ex: new EntityNotFoundException("Job", "ext-1"));
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        await sut.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UnhandledException_ShouldReturn500()
    {
        var sut = CreateSut(ex: new InvalidOperationException("unexpected"));
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        await sut.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task NoException_ShouldPassThrough()
    {
        var sut = CreateSut(thrownStatusCode: 200);
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        await sut.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(200);
    }
}
