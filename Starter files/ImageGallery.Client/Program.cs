using ImageGallery.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(configure => 
        configure.JsonSerializerOptions.PropertyNamingPolicy = null);

JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAccessTokenManagement();

// create an HttpClient used for accessing the API
builder.Services.AddHttpClient("APIClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ImageGalleryAPIRoot"]);
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
}).AddUserAccessTokenHandler();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;

    }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.AccessDeniedPath = "/Authentication/AccessDenied";

    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Authority = "https://localhost:5001/";
        options.ClientId = "imagegalleryclient";
        options.ClientSecret = "secret";
        options.ResponseType = "code";


        options.Scope.Add("imagegalleryapi.fullaccess");
        //following are requested by default
        //options.Scope.Add("openid");
        //options.Scope.Add("profile");
        options.Scope.Add("roles");
        options.Scope.Add("country");
        //mapping is needed manually as dotnet core does not do for custom idenityresource
        options.ClaimActions.MapJsonKey("role", "role"); //not using unique as there could be multiple roles
        options.ClaimActions.MapUniqueJsonKey("country", "country");
        //options.CallbackPath = new PathString("signin-oidc")
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        // Remove here means keep the claim, very confusing naming though
        options.ClaimActions.Remove("aud");
        options.ClaimActions.DeleteClaim("sid");
        options.ClaimActions.DeleteClaim("idp");
        options.TokenValidationParameters = new()
        {
            NameClaimType = "given_name",
            RoleClaimType = "role"
        };

    });

builder.Services.AddAuthorization(authorizationOptions =>
{
    authorizationOptions.AddPolicy("UserCanAddImage", AuthorizationPolicies.CanAddImage());
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Gallery}/{action=Index}/{id?}");

app.Run();
