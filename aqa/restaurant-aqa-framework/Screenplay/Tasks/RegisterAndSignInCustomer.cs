using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.Abilities;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class RegisterAndSignInCustomer(string? name = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var authApi = new AuthApi(actor.UsingTheApi());
        var registerRequest = SampleDataFactory.RegisterRequest();
        var regResponse = await authApi.RegisterAsync(registerRequest);
        if (!regResponse.Ok)
            throw new InvalidOperationException(
                $"Customer registration failed with {regResponse.Status}: {await regResponse.ReadErrorMessageAsync()}");

        var signInResponse = await authApi.SignInAsync(new SignInRequest(registerRequest.Email, registerRequest.Password));
        if (!signInResponse.Ok)
            throw new InvalidOperationException(
                $"Customer sign-in failed with {signInResponse.Status}: {await signInResponse.ReadErrorMessageAsync()}");

        var tokens = await signInResponse.ReadAsAsync<SignInResponse>();
        var session = new CustomerSession(registerRequest.Email, registerRequest.Password, tokens.IdToken);

        if (name is null)
            actor.Remember(session);
        else
            actor.Remember(name, session);

        if (actor.Can<IBrowseTheWeb>())
        {
            var page = actor.LookingAtThePage();
            var loginPage = new LoginPage(page);
            await loginPage.NavigateAsync();
            await loginPage.SignInAsync(registerRequest.Email, registerRequest.Password);
            await page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.Ordinal));
        }
    }
}
