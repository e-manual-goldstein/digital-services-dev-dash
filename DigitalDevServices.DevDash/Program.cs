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

var app = builder.Build();

DevDashDataServiceCollectionExtensions.EnsureDevDashDatabaseCreated(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
