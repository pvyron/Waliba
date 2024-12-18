using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pvyron.Site.Components;
using Pvyron.Site.Components.Account;
using Pvyron.Site.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<WalibaUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 4;
        options.Password.RequiredUniqueChars = 0;
        options.Password.RequireDigit = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<WalibaUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();

    await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await dbContext.Database.MigrateAsync();

    using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WalibaUser>>();
    using var userStore = scope.ServiceProvider.GetRequiredService<IUserStore<WalibaUser>>();

    var adminUser = await userManager.FindByEmailAsync(builder.Configuration["AdminCredentials:Email"]!);
            
    if (adminUser is not null)
    {
        await userManager.DeleteAsync(adminUser);
    }

    adminUser = Activator.CreateInstance<WalibaUser>();

    await userStore.SetUserNameAsync(adminUser, builder.Configuration["AdminCredentials:Email"],
        CancellationToken.None);
    await ((IUserEmailStore<WalibaUser>)userStore).SetEmailAsync(adminUser,
        builder.Configuration["AdminCredentials:Email"], CancellationToken.None);
    
    var result = await userManager.CreateAsync(adminUser, builder.Configuration["AdminCredentials:Password"]!);

    if (!result.Succeeded)
    {
        throw new Exception(string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)));
    }

    result = await userManager.ConfirmEmailAsync(adminUser, 
        await userManager.GenerateEmailConfirmationTokenAsync(adminUser));
    
    if (!result.Succeeded)
    {
        throw new Exception(string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)));
    }
    
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();