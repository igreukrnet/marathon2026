using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui.Components;

namespace Restaurant_Aqa_Framework.Ui;

public sealed class GroupOrderPage(IPage page) : BasePage(page)
{
    public HeaderComponent Header { get; } = new(page);

    public ILocator PageTitle => Page.Locator("h1.page-title");
    public ILocator StatusBadge => Page.Locator(".status-badge");
    public ILocator RefreshButton => Page.Locator("button.refresh-button");
    public ILocator WarningBanner => Page.Locator(".warning-banner");
    public ILocator EmptyNotice => Page.Locator(".empty-state-notice");
    public ILocator ErrorState => Page.Locator(".state-container.error-state");

    public ILocator ParticipantCards => Page.Locator("ul.participants-list > li.participant-item app-participant-card");
    public ILocator ConsolidatedItems => Page.Locator("ul.consolidated-dishes-list > li.consolidated-dish-item");
    public ILocator ConsolidatedEmptyNotice => Page.Locator(".consolidated-empty");
    public ILocator GrandTotalPrice => Page.Locator(".grand-total-price");
    public ILocator SubmitPreOrderButton => Page.Locator("button.submit-action-button");

    public ILocator SubmitModal => Page.Locator("app-modal").Filter(new() { HasText = "Submit Pre-Order" });
    public ILocator ConfirmSubmitButton => SubmitModal.Locator(".dialog-buttons button.button--primary");
    public ILocator CancelSubmitButton => SubmitModal.Locator(".dialog-buttons button.button--secondary");

    public ILocator RemoveGuestModal => Page.Locator("app-modal").Filter(new() { HasText = "Remove Guest" });
    public ILocator ConfirmRemoveButton => RemoveGuestModal.Locator("button.remove-confirm-button");
    public ILocator CancelRemoveButton => RemoveGuestModal.Locator(".dialog-buttons button.button--secondary");

    public Task NavigateAsync(string reservationId) => NavigateToAsync($"/reservations/{reservationId}/group-order");

    public ParticipantCardComponent Participant(int index) => new(ParticipantCards.Nth(index));

    public ParticipantCardComponent ParticipantByName(string name) =>
        new(ParticipantCards.Filter(new() { Has = Page.Locator("h2.title", new() { HasText = name }) }));

    public async Task OpenSubmitModalAsync() => await SubmitPreOrderButton.ClickAsync();

    public async Task ConfirmSubmitAsync()
    {
        await OpenSubmitModalAsync();
        await ConfirmSubmitButton.ClickAsync();
    }

    public async Task ConfirmRemoveGuestAsync(string guestName)
    {
        var card = ParticipantByName(guestName);
        await card.RemoveAsync();
        await ConfirmRemoveButton.ClickAsync();
    }
}
