using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Handlers.GuestSessions;
using APIHandler.Models.GroupReservation;
using APIHandler.Services.GuestSessions;
using AutoFixture.Xunit2;
using Moq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace APIHandler.Tests.Handlers;

public class SubmitGroupPreOrderHandlerTests
{
    private readonly Mock<IGuestSessionService> _mockGuestSessionService = new();
    private readonly Mock<ILambdaContext> _mockLambdaContext = new();
    private readonly Mock<ILambdaLogger> _mockLogger = new();
    private readonly SubmitGroupPreOrderHandler _handler;

    public SubmitGroupPreOrderHandlerTests()
    {
        _mockLambdaContext.Setup(c => c.Logger).Returns(_mockLogger.Object);
        _handler = new SubmitGroupPreOrderHandler(_mockGuestSessionService.Object);
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
    public async Task HandleRequest_EmptyBody_Returns400()
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
                        { "sub", "user-1" }
                    }
                }
            },
            Body = ""
        };

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task HandleRequest_MissingReservationIdInBody_Returns400()
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
                        { "sub", "user-1" }
                    }
                }
            },
            Body = JsonSerializer.Serialize(new { reservationId = "" })
        };

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(400, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_ValidRequest_Returns200WithConsolidatedDishes(
        string userId,
        string groupReservationId,
        string reservationId)
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
            },
            Body = JsonSerializer.Serialize(new { reservationId = reservationId })
        };

        var expectedResponse = new List<ConsolidatedDishResponse>
        {
            new("dish1", 3),
            new("dish2", 2)
        };

        _mockGuestSessionService
            .Setup(s => s.SubmitGroupPreOrderAsync(userId, groupReservationId, reservationId))
            .ReturnsAsync(expectedResponse);

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(200, response.StatusCode);
        Assert.Contains("dish1", response.Body);
        Assert.Contains("dish2", response.Body);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_ReservationNotFound_Returns404(
        string userId,
        string groupReservationId,
        string reservationId)
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
            },
            Body = JsonSerializer.Serialize(new { reservationId = reservationId })
        };

        _mockGuestSessionService
            .Setup(s => s.SubmitGroupPreOrderAsync(userId, groupReservationId, reservationId))
            .ThrowsAsync(new ReservationNotFoundException("Reservation not found."));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(404, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_GroupReservationNotFound_Returns404(
        string userId,
        string groupReservationId,
        string reservationId)
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
            },
            Body = JsonSerializer.Serialize(new { reservationId = reservationId })
        };

        _mockGuestSessionService
            .Setup(s => s.SubmitGroupPreOrderAsync(userId, groupReservationId, reservationId))
            .ThrowsAsync(new GroupReservationNotFoundException("Group reservation not found."));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(404, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_CallerNotOwner_Returns403(
        string userId,
        string groupReservationId,
        string reservationId)
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
            },
            Body = JsonSerializer.Serialize(new { reservationId = reservationId })
        };

        _mockGuestSessionService
            .Setup(s => s.SubmitGroupPreOrderAsync(userId, groupReservationId, reservationId))
            .ThrowsAsync(new ActionForbiddenException("Only the reservation owner can perform this action."));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(403, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_ValidationException_Returns400(
        string userId,
        string groupReservationId,
        string reservationId)
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
            },
            Body = JsonSerializer.Serialize(new { reservationId = reservationId })
        };

        _mockGuestSessionService
            .Setup(s => s.SubmitGroupPreOrderAsync(userId, groupReservationId, reservationId))
            .ThrowsAsync(new ValidationException("Validation error"));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(400, response.StatusCode);
    }
}
