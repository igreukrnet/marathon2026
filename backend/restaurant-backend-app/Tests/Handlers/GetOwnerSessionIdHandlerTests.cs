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

public class GetOwnerSessionIdHandlerTests
{
    private readonly Mock<IGuestSessionService> _mockGuestSessionService = new();
    private readonly Mock<ILambdaContext> _mockLambdaContext = new();
    private readonly Mock<ILambdaLogger> _mockLogger = new();
    private readonly GetOwnerSessionIdHandler _handler;

    public GetOwnerSessionIdHandlerTests()
    {
        _mockLambdaContext.Setup(c => c.Logger).Returns(_mockLogger.Object);
        _handler = new GetOwnerSessionIdHandler(_mockGuestSessionService.Object);
    }

    [Fact]
    public async Task HandleRequest_MissingReservationId_Returns400()
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
                { "reservationId", "res-1" }
            },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = null
            }
        };

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(401, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_ValidRequest_Returns200WithGuestSessionId(
        string userId,
        string reservationId,
        GetOwnerSessionIdResponse expectedResponse)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "reservationId", reservationId }
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
            .Setup(s => s.GetOwnerSessionIdAsync(userId, reservationId))
            .ReturnsAsync(expectedResponse);

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(200, response.StatusCode);
        Assert.Contains(expectedResponse.GuestSessionId, response.Body);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_ReservationNotFound_Returns404(string userId, string reservationId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "reservationId", reservationId }
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
            .Setup(s => s.GetOwnerSessionIdAsync(userId, reservationId))
            .ThrowsAsync(new ReservationNotFoundException("Reservation not found."));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(404, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_GroupReservationNotFound_Returns404(string userId, string reservationId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "reservationId", reservationId }
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
            .Setup(s => s.GetOwnerSessionIdAsync(userId, reservationId))
            .ThrowsAsync(new GroupReservationNotFoundException("Group reservation not found."));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(404, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_NotOwner_Returns403(string userId, string reservationId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "reservationId", reservationId }
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
            .Setup(s => s.GetOwnerSessionIdAsync(userId, reservationId))
            .ThrowsAsync(new ActionForbiddenException("Only the reservation owner can perform this action."));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(403, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_ValidationException_Returns400(string userId, string reservationId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "reservationId", reservationId }
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
            .Setup(s => s.GetOwnerSessionIdAsync(userId, reservationId))
            .ThrowsAsync(new ValidationException("Validation error"));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(400, response.StatusCode);
    }
}
