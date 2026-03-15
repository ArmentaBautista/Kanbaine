using KanbanRedmine.Components;
using KanbanRedmine.Models;
using KanbanRedmine.Services;
using KanbanRedmine.State;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Redmine
builder.Services.Configure<RedmineSettings>(builder.Configuration.GetSection(RedmineSettings.SectionName));

// HttpClient para comunicación con Redmine
builder.Services.AddHttpClient("RedmineClient", client =>
{
    var redmineSettings = builder.Configuration.GetSection(RedmineSettings.SectionName).Get<RedmineSettings>();
    if (redmineSettings != null)
    {
        client.BaseAddress = new Uri(redmineSettings.GetApiUrl());
        client.Timeout = TimeSpan.FromSeconds(redmineSettings.TimeoutSeconds);
    }
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Servicios de la aplicación
builder.Services.AddScoped<IRedmineService, RedmineService>();
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<NotificationService>();

// Autenticación para Blazor
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
    provider.GetRequiredService<CustomAuthenticationStateProvider>());

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
