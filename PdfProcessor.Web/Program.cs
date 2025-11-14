using PdfProcessor.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ✅ ADICIONAR: Registrar HttpClient para fazer chamadas à API
builder.Services.AddHttpClient("PdfProcessorAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:5239"); // URL da API
    client.Timeout = TimeSpan.FromMinutes(10); // Timeout para uploads grandes
});

// ✅ ADICIONAR: HttpClient padrão (para injeção direta)
builder.Services.AddScoped(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return httpClientFactory.CreateClient("PdfProcessorAPI");
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

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();