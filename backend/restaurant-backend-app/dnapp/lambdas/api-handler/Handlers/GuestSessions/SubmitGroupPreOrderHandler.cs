using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.GroupReservation;
using APIHandler.Services.GuestSessions;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GuestSessions;

public class SubmitGroupPreOrderHandler(IGuestSessionService guestSessionService) : IApiRequestHandler
{
    private readonly IGuestSessionService _guestSessionService = guestSessionService ?? throw new ArgumentNullException(nameof(guestSessionService));

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("groupReservationId", out string? groupReservationId) || string.IsNullOrWhiteSpace(groupReservationId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'groupReservationId' in path parameters.", context.Logger);
        }

        if (request.RequestContext?.Authorizer?.Claims == null ||
            !request.RequestContext.Authorizer.Claims.TryGetValue("sub", out var userId) ||
            string.IsNullOrWhiteSpace(userId))
        {
            return ResponseBuilder.CreateErrorResponse(401, "Unauthorized.", context.Logger);
        }

        if (!TryDeserializeRequest(request, out var submitRequest) || string.IsNullOrWhiteSpace(submitRequest?.ReservationId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Invalid request body: 'reservationId' is required.", context.Logger);
        }

        try
        {
            var response = await _guestSessionService.SubmitGroupPreOrderAsync(userId, groupReservationId, submitRequest.ReservationId);
            return ResponseBuilder.CreateJsonResponse(200, response);
        }
        catch (ReservationNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
        }
        catch (GroupReservationNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
        }
        catch (ActionForbiddenException ex)
        {
            return ResponseBuilder.CreateErrorResponse(403, ex.Message, context.Logger);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }
    }

    private static bool TryDeserializeRequest(APIGatewayProxyRequest request, out SubmitGroupPreOrderRequest? submitRequest)
    {
        if (string.IsNullOrWhiteSpace(request?.Body))
        {
            submitRequest = null;
            return false;
        }

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            submitRequest = JsonSerializer.Deserialize<SubmitGroupPreOrderRequest>(request.Body, options);
            return submitRequest != null;
        }
        catch (JsonException)
        {
            submitRequest = null;
            return false;
        }
    }
}
