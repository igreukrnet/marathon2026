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

public class DeleteGuestSessionHandlerTests
{
    private readonly Mock<IGuestSessionService> _mockGuestSessionService = new();
    private readonly Mock<ILambdaContext> _mockLambdaContext = new();
    private readonly Mock<ILambdaLogger> _mockLogger = new();
    private readonly DeleteGuestSessionHandler _handler;

    public DeleteGuestSessionHandlerTests()
    {
        _mockLambdaContext.Setup(c => c.Logger).Returns(_mockLogger.Object);
        _handler = new DeleteGuestSessionHandler(_mockGuestSessionService.Object);
    }

    [Fact]
    public async Task HandleRequest_MissingGroupReservationId_Returns400()
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "guestSessionId", "session-1" }
            }
        };

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task HandleRequest_MissingGuestSessionId_Returns400()
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", "group-1" }
            }
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
                { "groupReservationId", "group-1" },
                { "guestSessionId", "session-1" }
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
                { "groupReservationId", "group-1" },
                { "guestSessionId", "session-1" }
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
    public async Task HandleRequest_ValidRequest_Returns200WithUpdatedSnapshot(
        string userId,
        string groupReservationId,
        string guestSessionId,
        GetAllSessionsResponse updatedSnapshot)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", groupReservationId },
                { "guestSessionId", guestSessionId }
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
            .Setup(s => s.DeleteGuestSessionAsync(userId, groupReservationId, guestSessionId))
            .ReturnsAsync(updatedSnapshot);

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        _mockGuestSessionService.Verify(
            s => s.DeleteGuestSessionAsync(userId, groupReservationId, guestSessionId),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_GroupReservationNotFound_Returns404(
        string userId,
        string groupReservationId,
        string guestSessionId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", groupReservationId },
                { "guestSessionId", guestSessionId }
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
            .Setup(s => s.DeleteGuestSessionAsync(userId, groupReservationId, guestSessionId))
            .ThrowsAsync(new GroupReservationNotFoundException("Group not found"));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(404, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_GuestSessionNotFound_Returns404(
        string userId,
        string groupReservationId,
        string guestSessionId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", groupReservationId },
                { "guestSessionId", guestSessionId }
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
            .Setup(s => s.DeleteGuestSessionAsync(userId, groupReservationId, guestSessionId))
            .ThrowsAsync(new GuestSessionNotFoundException("Session not found"));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(404, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_ReservationNotFound_Returns404(
        string userId,
        string groupReservationId,
        string guestSessionId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", groupReservationId },
                { "guestSessionId", guestSessionId }
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
            .Setup(s => s.DeleteGuestSessionAsync(userId, groupReservationId, guestSessionId))
            .ThrowsAsync(new ReservationNotFoundException("Reservation not found"));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(404, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_Forbidden_Returns403(
        string userId,
        string groupReservationId,
        string guestSessionId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", groupReservationId },
                { "guestSessionId", guestSessionId }
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
            .Setup(s => s.DeleteGuestSessionAsync(userId, groupReservationId, guestSessionId))
            .ThrowsAsync(new ActionForbiddenException("Only owner can delete sessions"));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(403, response.StatusCode);
    }

    [Theory]
    [AutoData]
    public async Task HandleRequest_ValidationError_SessionDoesNotBelongToGroup_Returns400(
        string userId,
        string groupReservationId,
        string guestSessionId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "groupReservationId", groupReservationId },
                { "guestSessionId", guestSessionId }
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
            .Setup(s => s.DeleteGuestSessionAsync(userId, groupReservationId, guestSessionId))
            .ThrowsAsync(new ValidationException("Guest session does not belong to this group reservation"));

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(400, response.StatusCode);
    }
}
