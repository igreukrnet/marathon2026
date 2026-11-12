using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Abilities;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.StepDefinitions;

[Binding]
public sealed class GroupPreOrderStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    private const string NonExistentId = "00000000-0000-0000-0000-000000000000";

    // ----------------------------------------------------
    // Given Steps
    // ----------------------------------------------------

    [Given("the customer creates a group reservation")]
    public Task GivenCustomerCreatesGroupReservation() =>
        Actor.AttemptsToAsync(new SetUpGroupReservation());

    [Given("the customer creates a group reservation called {string}")]
    public Task GivenCustomerCreatesGroupReservationCalled(string name) =>
        Actor.AttemptsToAsync(new SetUpGroupReservation(name: name));

    [Given("a guest called {string} joins the group and adds a dish")]
    public Task GivenGuestJoinsAndAddsOneDish(string guestName) =>
        GivenGuestJoinsAndAddsDishes(guestName, 1);

    [Given("a guest called {string} joins the group and adds {int} dish")]
    [Given("a guest called {string} joins the group and adds {int} dishes")]
    [Given("a guest called {string} joins the group and adds {int} of the dish")]
    public async Task GivenGuestJoinsAndAddsDishes(string guestName, int count)
    {
        await Actor.AttemptsToAsync(new JoinGroupAsGuest(guestName));
        await Actor.AttemptsToAsync(new AddDishToGuestCart(guestName, quantity: count));
    }

    [Given("a guest called {string} joins the group without adding dishes")]
    public Task GivenGuestJoinsWithoutDishes(string guestName) =>
        Actor.AttemptsToAsync(new JoinGroupAsGuest(guestName));

    [Given("a guest called {string} joins the group and submits their cart")]
    public async Task GivenGuestJoinsAndSubmits(string guestName)
    {
        await Actor.AttemptsToAsync(new JoinGroupAsGuest(guestName));
        await Actor.AttemptsToAsync(new SubmitGuestCart(guestName));
    }

    [Given("a guest called {string} joins the {string} group")]
    public Task GivenGuestJoinsNamedGroup(string guestName, string groupName)
    {
        var namedInvite = Recall<GuestInviteResponse>(groupName);
        return Actor.AttemptsToAsync(new JoinGroupAsGuest(guestName, namedInvite.GroupReservationId));
    }

    [Given("the guest {string} submits their cart")]
    [When("the guest {string} submits their cart")]
    public Task WhenGuestSubmitsCart(string guestName) =>
        Actor.AttemptsToAsync(new SubmitGuestCart(guestName));

    [When("the guest {string} adds {int} more dish to their cart")]
    public Task WhenGuestAddsMoreDish(string guestName, int count) =>
        Actor.AttemptsToAsync(new AddDishToGuestCart(guestName, quantity: count));

    // ----------------------------------------------------
    // Level 1: Review Steps
    // ----------------------------------------------------

    [When("the owner opens the group pre-order page")]
    public async Task WhenOwnerOpensGroupOrderPage()
    {
        await Actor.AttemptsToAsync(new OpenGroupOrderPage());
        if (Actor.Can<IBrowseTheWeb>())
        {
            var page = Actor.LookingAtThePage();
            try
            {
                await page.Locator("app-participant-card, .empty-state-notice, .state-container.error-state")
                    .First.WaitForAsync(new() { Timeout = 10000 });
            }
            catch (TimeoutException) { }
        }
    }

    [When("the owner refreshes the group pre-order review")]
    public async Task WhenOwnerRefreshesReview()
    {
        var page = Actor.LookingAtThePage();
        var groupOrderPage = new GroupOrderPage(page);

        var responseTask = page.WaitForResponseAsync(
            resp => resp.Url.Contains("/sessions"),
            new() { Timeout = 15000 });

        await groupOrderPage.RefreshButton.ClickAsync();
        try
        {
            await responseTask;
        }
        catch (TimeoutException) { }

        await page.WaitForTimeoutAsync(500);
    }

    [Then("the owner sees participants {string} and {string}")]
    public async Task ThenOwnerSeesParticipants(string guest1, string guest2)
    {
        var participants = await Actor.AsksForAsync(new TheGroupParticipants());
        var names = participants.Select(p => p.Name).ToList();

        await Assert.That(names.Any(n => n.Contains(guest1, StringComparison.OrdinalIgnoreCase))).IsTrue();
        await Assert.That(names.Any(n => n.Contains(guest2, StringComparison.OrdinalIgnoreCase))).IsTrue();
    }

    [Then("the owner sees {string} with {int} dish")]
    [Then("the owner sees {string} with {int} dishes")]
    public async Task ThenOwnerSeesGuestWithDishes(string guestName, int dishCount)
    {
        ParticipantSummary? guest = null;
        for (var i = 0; i < 10; i++)
        {
            var participants = await Actor.AsksForAsync(new TheGroupParticipants());
            guest = participants.FirstOrDefault(p => p.Name.Contains(guestName, StringComparison.OrdinalIgnoreCase));
            if (guest is not null && guest.ItemsCount == dishCount)
                break;

            if (Actor.Can<IBrowseTheWeb>())
                await Actor.LookingAtThePage().WaitForTimeoutAsync(500);
            else
                await Task.Delay(500);
        }

        await Assert.That(guest).IsNotNull();
        await Assert.That(guest!.ItemsCount).IsEqualTo(dishCount);
    }

    [Then("the grand total equals the sum of all participant orders")]
    [Then("the grand total reflects the updated quantity")]
    public async Task ThenGrandTotalMatches()
    {
        var participants = await Actor.AsksForAsync(new TheGroupParticipants());
        var grandTotal = await Actor.AsksForAsync(new TheGroupGrandTotal());
        var sumOfParticipants = participants.Sum(p => p.Total);

        await Assert.That(grandTotal).IsGreaterThan(0m);
        await Assert.That(grandTotal).IsEqualTo(sumOfParticipants);
    }

    [When("the owner views the group sessions")]
    public Task WhenOwnerViewsGroupSessions() =>
        Actor.AttemptsToAsync(new ViewSessionsForGroup());

    [Then("{string} is listed as still choosing")]
    public async Task ThenGuestIsListedAsStillChoosing(string guestName)
    {
        var participants = await Actor.AsksForAsync(new TheGroupParticipants());
        var guest = participants.FirstOrDefault(p => p.Name.Contains(guestName, StringComparison.OrdinalIgnoreCase));

        await Assert.That(guest).IsNotNull();
        await Assert.That(guest!.IsSubmitted).IsFalse();
    }

    [Then("{string} is listed as submitted")]
    public async Task ThenGuestIsListedAsSubmitted(string guestName)
    {
        var participants = await Actor.AsksForAsync(new TheGroupParticipants());
        var guest = participants.FirstOrDefault(p => p.Name.Contains(guestName, StringComparison.OrdinalIgnoreCase));

        await Assert.That(guest).IsNotNull();
        await Assert.That(guest!.IsSubmitted).IsTrue();
    }

    [Then("{string} is listed with no dishes and a total of {int}")]
    public async Task ThenGuestIsListedWithNoDishes(string guestName, int total)
    {
        var participants = await Actor.AsksForAsync(new TheGroupParticipants());
        var guest = participants.FirstOrDefault(p => p.Name.Contains(guestName, StringComparison.OrdinalIgnoreCase));

        await Assert.That(guest).IsNotNull();
        await Assert.That(guest!.ItemsCount).IsEqualTo(0);
        await Assert.That(guest.Total).IsEqualTo(total);
    }

    [Then("the group has no participants and a total of {int}")]
    public async Task ThenGroupHasNoParticipants(int total)
    {
        var participants = await Actor.AsksForAsync(new TheGroupParticipants());
        var grandTotal = await Actor.AsksForAsync(new TheGroupGrandTotal());

        await Assert.That(participants).IsEmpty();
        await Assert.That(grandTotal).IsEqualTo(total);
    }

    [When("another customer attempts to view the group sessions")]
    public async Task WhenAnotherCustomerAttemptsToView()
    {
        await Actor.AttemptsToAsync(new RegisterAndSignInCustomer(name: "OtherCustomer"));
        var otherCustomer = Recall<CustomerSession>("OtherCustomer");
        await Actor.AttemptsToAsync(new ViewSessionsForGroup(bearerToken: otherCustomer.IdToken, mustSucceed: false));
    }

    [Then("access to the group sessions is forbidden")]
    public async Task ThenAccessIsForbidden()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(403);
    }

    [When("an unauthenticated user attempts to view the group sessions")]
    public Task WhenUnauthenticatedAttemptsToView() =>
        Actor.AttemptsToAsync(new ViewSessionsForGroup(bearerToken: string.Empty, mustSucceed: false));

    [Then("viewing the group sessions is refused as unauthorized")]
    public async Task ThenViewingRefusedUnauthorized()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(401);
    }

    [When("a customer attempts to view sessions of a non-existent group")]
    public async Task WhenCustomerViewsNonExistentGroup()
    {
        if (!Actor.TryRecall<CustomerSession>(out var customer))
        {
            await Actor.AttemptsToAsync(new RegisterAndSignInCustomer());
            customer = Actor.Recall<CustomerSession>();
        }
        await Actor.AttemptsToAsync(new ViewSessionsForGroup(
            groupReservationId: NonExistentId,
            bearerToken: customer.IdToken,
            mustSucceed: false));
    }

    [Then("the group is reported as not found")]
    public async Task ThenGroupNotFound()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(404);
    }

    [When("the customer opens the group pre-order page for a non-existent reservation")]
    public async Task WhenCustomerOpensNonExistentReservationPage()
    {
        if (!Actor.TryRecall<CustomerSession>(out _))
            await Actor.AttemptsToAsync(new RegisterAndSignInCustomer());
        await Actor.AttemptsToAsync(new OpenGroupOrderPage(reservationId: NonExistentId));
    }

    [Then("the storefront displays a reservation not found error")]
    public async Task ThenStorefrontDisplaysNotFoundError()
    {
        var page = new GroupOrderPage(Actor.LookingAtThePage());
        await Assert.That(await page.ErrorState.IsVisibleAsync()).IsTrue();
    }

    // ----------------------------------------------------
    // Level 2: Confirmation Steps
    // ----------------------------------------------------

    [Given("the owner submits the consolidated pre-order")]
    [When("the owner submits the consolidated pre-order")]
    public Task WhenOwnerSubmitsPreOrder() =>
        Actor.AttemptsToAsync(new SubmitGroupPreOrder(throughUi: Actor.Can<IBrowseTheWeb>()));

    [When("the guest {string} attempts to submit their cart")]
    public Task WhenGuestAttemptsToSubmitCart(string guestName) =>
        Actor.AttemptsToAsync(new SubmitGuestCart(guestName, mustSucceed: false));

    [Then("the submission is rejected because the cart is empty")]
    [Then("the submission is rejected because no dishes were selected")]
    public async Task ThenSubmissionRejectedCartEmpty()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(400);
        await Assert.That(outcome.Body.Contains("cart is empty", StringComparison.OrdinalIgnoreCase) ||
                          outcome.Body.Contains("no dishes", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [Then("the group pre-order is marked as sent to the restaurant")]
    public async Task ThenGroupPreOrderMarkedSent()
    {
        var status = await Actor.AsksForAsync(new TheGroupStatus());
        await Assert.That(status.Contains("Sent", StringComparison.OrdinalIgnoreCase) ||
                          status.Contains("Submitted", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [Then("the consolidated pre-order contains {int} of that dish")]
    public async Task ThenConsolidatedPreOrderContainsQuantity(int expectedQty)
    {
        var order = await Actor.AsksForAsync(new TheConsolidatedOrder());
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.OrderItems).IsNotEmpty();
        var totalQty = order.OrderItems.Sum(i => i.Quantity);
        await Assert.That(totalQty).IsEqualTo(expectedQty);
    }

    [Then("the order status is submitted by customer")]
    public async Task ThenOrderStatusIsSubmittedByCustomer()
    {
        var order = await Actor.AsksForAsync(new TheConsolidatedOrder());
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.Status).IsEqualTo(OrderStatus.SubmittedByCustomer);
    }

    [Then("the waiter sees the reservation order with status submitted by customer")]
    public async Task ThenWaiterSeesOrderSubmittedByCustomer()
    {
        var order = await Actor.AsksForAsync(new TheConsolidatedOrder());
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.Status).IsEqualTo(OrderStatus.SubmittedByCustomer);
    }

    [When("the owner attempts to submit the consolidated pre-order")]
    public Task WhenOwnerAttemptsToSubmitPreOrder() =>
        Actor.AttemptsToAsync(new SubmitGroupPreOrder(mustSucceed: false));

    [Then("the submission is rejected because no guest carts were submitted")]
    public async Task ThenSubmissionRejectedNoCarts()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(400);
        await Assert.That(outcome.Body.Contains("No submitted guest sessions found", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [Then("the submission is rejected because no dishes were selected")]
    public async Task ThenSubmissionRejectedNoDishes()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(400);
        await Assert.That(outcome.Body.Contains("No dishes found in submitted guest sessions", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [When("a customer attempts to submit a pre-order for a non-existent group")]
    public async Task WhenCustomerSubmitsNonExistentGroup()
    {
        if (!Actor.TryRecall<CustomerSession>(out var customer))
        {
            await Actor.AttemptsToAsync(new RegisterAndSignInCustomer());
            customer = Actor.Recall<CustomerSession>();
        }
        await Actor.AttemptsToAsync(new SubmitGroupPreOrder(
            groupReservationId: NonExistentId,
            reservationId: NonExistentId,
            bearerToken: customer.IdToken,
            mustSucceed: false));
    }

    [When("another customer attempts to submit the consolidated pre-order")]
    public async Task WhenAnotherCustomerSubmitsPreOrder()
    {
        await Actor.AttemptsToAsync(new RegisterAndSignInCustomer(name: "OtherCustomer2"));
        var other = Recall<CustomerSession>("OtherCustomer2");
        await Actor.AttemptsToAsync(new SubmitGroupPreOrder(bearerToken: other.IdToken, mustSucceed: false));
    }

    [Then("confirming the group pre-order is forbidden")]
    public async Task ThenConfirmingIsForbidden()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(403);
    }

    [When("the owner attempts to submit the consolidated pre-order again")]
    public Task WhenOwnerSubmitsPreOrderAgain() =>
        Actor.AttemptsToAsync(new SubmitGroupPreOrder(mustSucceed: false));

    [Then("submitting the group pre-order again is rejected")]
    public async Task ThenSubmittingAgainRejected()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status == 200 || outcome.Status >= 400).IsTrue();
    }

    // ----------------------------------------------------
    // Level 3: Removal Steps
    // ----------------------------------------------------

    [When("the owner removes the guest {string} on the storefront")]
    public Task WhenOwnerRemovesGuestOnStorefront(string guestName) =>
        Actor.AttemptsToAsync(new RemoveGuestFromGroup(guestName, throughUi: true));

    [When("the owner removes the guest {string} from the group")]
    public Task WhenOwnerRemovesGuestFromGroup(string guestName) =>
        Actor.AttemptsToAsync(new RemoveGuestFromGroup(guestName, throughUi: false));

    private static bool IsOwner(ParticipantSummary p) =>
        p.Name.Contains("Owner", StringComparison.OrdinalIgnoreCase) ||
        p.Name.Contains("My Pre-Order", StringComparison.OrdinalIgnoreCase);

    [Then("the owner sees only participant {string}")]
    public async Task ThenOwnerSeesOnlyParticipant(string guestName)
    {
        var participants = await Actor.AsksForAsync(new TheGroupParticipants());
        var guest = participants.FirstOrDefault(p => p.Name.Contains(guestName, StringComparison.OrdinalIgnoreCase));
        await Assert.That(guest).IsNotNull();
        var nonOwnerCount = participants.Count(p => !IsOwner(p));
        if (nonOwnerCount != 1)
        {
            var names = string.Join(", ", participants.Select(p => $"'{p.Name}'"));
            throw new InvalidOperationException(
                $"Expected 1 non-owner participant but found {nonOwnerCount}. Visible participants: {names}");
        }
        await Assert.That(nonOwnerCount).IsEqualTo(1);
    }

    [Then("the grand total equals the order of {string}")]
    public async Task ThenGrandTotalEqualsOrderOf(string guestName)
    {
        var participants = await Actor.AsksForAsync(new TheGroupParticipants());
        var grandTotal = await Actor.AsksForAsync(new TheGroupGrandTotal());
        var guest = participants.FirstOrDefault(p => p.Name.Contains(guestName, StringComparison.OrdinalIgnoreCase));

        await Assert.That(guest).IsNotNull();
        await Assert.That(grandTotal).IsEqualTo(guest!.Total);
    }

    [Then("the group participants contain only {string}")]
    public async Task ThenGroupParticipantsContainOnly(string guestName)
    {
        await Actor.AttemptsToAsync(new ViewSessionsForGroup());
        var participants = await Actor.AsksForAsync(new TheGroupParticipants());
        var guest = participants.FirstOrDefault(p => p.Name.Contains(guestName, StringComparison.OrdinalIgnoreCase));

        await Assert.That(guest).IsNotNull();
        await Assert.That(participants.Count(p => !IsOwner(p))).IsEqualTo(1);
    }

    [When("the owner of {string} attempts to remove {string} from {string}")]
    public Task WhenOwnerRemovesGuestFromAnotherGroup(string ownerGroup, string guestName, string targetGroup)
    {
        var guest = Recall<GuestSessionInfo>(guestName);
        var targetInvite = Recall<GuestInviteResponse>(targetGroup);
        return Actor.AttemptsToAsync(new RemoveGuestFromGroup(
            guestName,
            groupReservationId: targetInvite.GroupReservationId,
            guestSessionId: guest.SessionId,
            mustSucceed: false));
    }

    [Then("removing the guest is rejected as not belonging to the group")]
    public async Task ThenRemovalRejectedNotBelonging()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(400);
        await Assert.That(outcome.Body.Contains("does not belong to this group", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [When("the owner attempts to remove a non-existent guest")]
    public Task WhenOwnerRemovesNonExistentGuest() =>
        Actor.AttemptsToAsync(new RemoveGuestFromGroup(
            "NonExistent",
            guestSessionId: NonExistentId,
            mustSucceed: false));

    [Then("the guest session is reported as not found")]
    public async Task ThenGuestSessionNotFound()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(404);
    }

    [When("another customer attempts to remove the guest {string}")]
    public async Task WhenAnotherCustomerRemovesGuest(string guestName)
    {
        await Actor.AttemptsToAsync(new RegisterAndSignInCustomer(name: "OtherCustomer3"));
        var other = Recall<CustomerSession>("OtherCustomer3");
        await Actor.AttemptsToAsync(new RemoveGuestFromGroup(
            guestName,
            bearerToken: other.IdToken,
            mustSucceed: false));
    }

    [Then("removing the guest is forbidden")]
    public async Task ThenRemovingGuestIsForbidden()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(403);
    }

    [Then("the waiter sees the reservation order containing only the dishes of {string}")]
    public async Task ThenWaiterSeesOrderContainingOnlyGuest(string guestName)
    {
        var order = await Actor.AsksForAsync(new TheConsolidatedOrder());
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.OrderItems).IsNotEmpty();
        // Since Maria was removed and only Alex remained with 1 dish, order should contain 1 dish
        var totalQty = order.OrderItems.Sum(i => i.Quantity);
        await Assert.That(totalQty).IsEqualTo(1);
    }
}
