using BlazorServerApp.Components;
using BlazorServerApp.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<CounterStore>();
builder.Services.AddSingleton<MemoStore>();
builder.Services.AddSingleton<PdfDocumentStore>();
builder.Services.AddSingleton<ThemeStore>();
builder.Services.AddScoped<DatabaseAdminService>();
builder.Services.AddScoped<LayoutState>();

// Allow larger PDF uploads to flow over the Blazor Server SignalR circuit.
builder.Services.Configure<HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 50 * 1024 * 1024; // 50 MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

var staticFileContentTypeProvider = new FileExtensionContentTypeProvider();
staticFileContentTypeProvider.Mappings[".ftl"] = "text/plain"; // pdf.js viewer localization files

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticFileContentTypeProvider,
    // Force PDFs to render inline (e.g. in our <iframe> viewer) instead of
    // some browsers showing a "click to open" placeholder or downloading them.
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers[HeaderNames.ContentDisposition] =
                new ContentDispositionHeaderValue("inline") { FileNameStar = ctx.File.Name }.ToString();
        }
    }
});
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
