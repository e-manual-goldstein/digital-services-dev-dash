using DigitalDevServices.Data;
using DigitalDevServices.Services;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

builder.Services.AddDevDashData(builder.Configuration);
builder.Services.AddEnvironmentServices(builder.Configuration);
builder.Services.AddPipelineFeedServices();
builder.Services.AddDeployableApplicationServices();
builder.Services.AddLogServices();
builder.Services.AddTextFormattingServices();
builder.Services.AddConfigurationServices();
builder.Services.AddGitHistoryServices();

var app = builder.Build();

DevDashDataServiceCollectionExtensions.EnsureDevDashDatabaseCreated(app.Services);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
