using Microsoft.Playwright;

namespace Restaurant_Aqa_Framework.Ui.Components;

public sealed class ParticipantCardComponent(ILocator root)
{
    public ILocator Title => root.Locator("h2.title");
    public ILocator SubmittedChip => root.Locator(".chip--submitted");
    public ILocator PendingChip => root.Locator(".chip--pending");
    public ILocator RemoveButton => root.Locator("button.remove-guest-btn");
    public ILocator EmptyNotice => root.Locator("p.empty");
    public ILocator SubtotalAmount => root.Locator(".subtotal-amount");
    public ILocator Items => root.Locator("ul.items > li.item");

    public async Task<string> GetTitleAsync() => (await Title.InnerTextAsync()).Trim();
    public async Task<string> GetSubtotalAsync() => (await SubtotalAmount.InnerTextAsync()).Trim();
    public async Task<bool> IsSubmittedAsync() => await SubmittedChip.IsVisibleAsync();
    public async Task<bool> IsStillChoosingAsync() => await PendingChip.IsVisibleAsync();
    public Task RemoveAsync() => RemoveButton.ClickAsync();
}
