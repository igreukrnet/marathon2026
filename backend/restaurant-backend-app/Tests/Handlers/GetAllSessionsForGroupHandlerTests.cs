using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Handlers.GuestSessions;
using APIHandler.Models.GuestSessions;
using APIHandler.Services.GuestSessions;
using AutoFixture.Xunit2;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace APIHandler.Tests.Handlers;

public class GetAllSessionsForGroupHandlerTests
{
    private readonly Mock<IGuestSessionService> _mockGuestSessionService = new();
    private readonly Mock<ILambdaContext> _mockLambdaContext = new();
    private readonly Mock<ILambdaLogger> _mockLogger = new();
    private readonly GetAllSessionsForGroupHandler _handler;

    public GetAllSessionsForGroupHandlerTests()
    {
        _mockLambdaContext.Setup(c => c.Logger).Returns(_mockLogger.Object);
        _handler = new GetAllSessionsForGroupHandler(_mockGuestSessionService.Object);
    }

    [Fact]
    public async Task HandleRequest_MissingGroupReservationId_Returns400()
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>()
        };

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task HandleRequest_MissingAuthorizer_Returns401()
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", "group-1" }
            },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = null
            }
        };

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(401, response.StatusCode);
    }

    [Fact]
    public async Task HandleRequest_MissingSubClaim_Returns401()
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", "group-1" }
            },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = new APIGatewayCustomAuthorizerContext
                {
                    Claims = new Dictionary<string, string>
                    {
                        { "email", "test@example.com" }
                    }
                }
            }
        };

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(401, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_ValidRequest_Returns200WithSessions(
        string userId,
        string groupReservationId,
        GetAllSessionsResponse sessionsResponse)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", groupReservationId }
            },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = new APIGatewayCustomAuthorizerContext
                {
                    Claims = new Dictionary<string, string>
                    {
                        { "sub", userId }
                    }
                }
            }
        };

        _mockGuestSessionService
            .Setup(s => s.GetAllSessionsAsync(userId, groupReservationId))
            .ReturnsAsync(sessionsResponse);

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_GroupNotFound_Returns404(string userId, string groupReservationId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", groupReservationId }
            },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = new APIGatewayCustomAuthorizerContext
                {
                    Claims = new Dictionary<string, string>
                    {
                        { "sub", userId }
                    }
                }
            }
        };

        _mockGuestSessionService
            .Setup(s => s.GetAllSessionsAsync(userId, groupReservationId))
            .ThrowsAsync(new GroupReservationNotFoundException());

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(404, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_ReservationNotFound_Returns404(string userId, string groupReservationId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", groupReservationId }
            },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = new APIGatewayCustomAuthorizerContext
                {
                    Claims = new Dictionary<string, string>
                    {
                        { "sub", userId }
                    }
                }
            }
        };

        _mockGuestSessionService
            .Setup(s => s.GetAllSessionsAsync(userId, groupReservationId))
            .ThrowsAsync(new ReservationNotFoundException("Parent reservation not found."));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(404, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_Forbidden_Returns403(string userId, string groupReservationId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", groupReservationId }
            },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = new APIGatewayCustomAuthorizerContext
                {
                    Claims = new Dictionary<string, string>
                    {
                        { "sub", userId }
                    }
                }
            }
        };

        _mockGuestSessionService
            .Setup(s => s.GetAllSessionsAsync(userId, groupReservationId))
            .ThrowsAsync(new ActionForbiddenException("Only the reservation owner can perform this action."));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(403, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_ValidationException_Returns400(string userId, string groupReservationId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", groupReservationId }
            },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = new APIGatewayCustomAuthorizerContext
                {
                    Claims = new Dictionary<string, string>
                    {
                        { "sub", userId }
                    }
                }
            }
        };

        _mockGuestSessionService
            .Setup(s => s.GetAllSessionsAsync(userId, groupReservationId))
            .ThrowsAsync(new ValidationException("Validation failed"));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(400, response.StatusCode);
    }
}
