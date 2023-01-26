using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using CP.Resumes.Helpers;
using Microsoft.EntityFrameworkCore;
using CP.Resumes.Data;

var builder = WebApplication.CreateBuilder(args);

var initialScopes = builder.Configuration["DownstreamApi:Scopes"]?.Split(' ') ?? builder.Configuration["MicrosoftGraph:Scopes"]?.Split(' ');

// Add services to the container.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
        .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
            .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
            .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
    options.AddPolicy("administrators", p => { p.RequireClaim("groups", SecurityGroup.AppAdministrators); });
    options.AddPolicy("recruiting", p => { p.RequireClaim("groups", SecurityGroup.AppRecruiting); });
    options.AddPolicy("hr", p => { p.RequireClaim("groups", SecurityGroup.AppHumanResources); });
    options.AddPolicy("managers", p => { p.RequireClaim("groups", SecurityGroup.AppManagers); });

    options.AddPolicy("hr", b => b.RequireAssertion(c =>
        c.User.HasClaim("groups", SecurityGroup.AppHumanResources)
    ));

    options.AddPolicy("recruiting", b => b.RequireAssertion(c =>
        c.User.HasClaim("groups", SecurityGroup.AppRecruiting)
    ));

    options.AddPolicy("managers", b => b.RequireAssertion(c =>
        c.User.HasClaim("groups", SecurityGroup.AppManagers)
    ));

    options.AddPolicy("hr,recruiting", b => b.RequireAssertion(c =>
        c.User.HasClaim("groups", SecurityGroup.AppHumanResources) ||
        c.User.HasClaim("groups", SecurityGroup.AppRecruiting)
    ));

    options.AddPolicy("hr,managers", b => b.RequireAssertion(c =>
        c.User.HasClaim("groups", SecurityGroup.AppHumanResources) ||
        c.User.HasClaim("groups", SecurityGroup.AppManagers)
    ));

    options.AddPolicy("recruiting,managers", b => b.RequireAssertion(c =>
        c.User.HasClaim("groups", SecurityGroup.AppRecruiting) ||
        c.User.HasClaim("groups", SecurityGroup.AppManagers)
    ));

    options.AddPolicy("hr,recruiting,managers", b => b.RequireAssertion(c =>
        c.User.HasClaim("groups", SecurityGroup.AppHumanResources) ||
        c.User.HasClaim("groups", SecurityGroup.AppRecruiting) ||
        c.User.HasClaim("groups", SecurityGroup.AppManagers)
    ));

    options.AddPolicy("administrators,hr,recruiting", b => b.RequireAssertion(c =>
        c.User.HasClaim("groups", SecurityGroup.AppAdministrators) ||
        c.User.HasClaim("groups", SecurityGroup.AppHumanResources) ||
        c.User.HasClaim("groups", SecurityGroup.AppRecruiting)
    ));

    options.AddPolicy("administrators,recruiting,managers", b => b.RequireAssertion(c =>
        c.User.HasClaim("groups", SecurityGroup.AppAdministrators) ||
        c.User.HasClaim("groups", SecurityGroup.AppManagers) ||
        c.User.HasClaim("groups", SecurityGroup.AppRecruiting)
    ));

    options.AddPolicy("super", b => b.RequireAssertion(c =>
        c.User.HasClaim("groups", SecurityGroup.AppAdministrators) ||
        c.User.HasClaim("groups", SecurityGroup.AppHumanResources) ||
        c.User.HasClaim("groups", SecurityGroup.AppManagers) ||
        c.User.HasClaim("groups", SecurityGroup.AppRecruiting)
    ));
});

//builder.Services.AddDbContext<ResumeContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ResumeContext")));

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseQueryStrings = true;
    options.LowercaseUrls = true;
});

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddRazorPages().AddMicrosoftIdentityUI();

//builder.Services.AddWebOptimizer(options =>
//{
    
//});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapControllers();

app.Run();
