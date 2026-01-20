using PdfProcessor.Core.Interfaces;
using PdfProcessor.Infrastructure.Parsers;
using PdfProcessor.Infrastructure.PdfServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar serviços do PDF Processor
builder.Services.AddScoped<IPdfRotateService, PdfRotateService>();
builder.Services.AddScoped<IPdfCompressService, PdfCompressService>();
builder.Services.AddScoped<IPdfMergeService, PdfMergeService>();
builder.Services.AddScoped<IItauMovimentacaoParser, ItauMovimentacaoParser>();
builder.Services.AddScoped<IItauCashParser, ItauCashParser>();
builder.Services.AddScoped<IBtgParser, BtgParser>();

// ✅ CORS DINÂMICO: Aceita qualquer rede local + localhost
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            // Parse a URL
            if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                var host = uri.Host;
                
                // ✅ Permitir localhost
                if (host == "localhost" || host == "127.0.0.1")
                    return true;
                
                // ✅ Permitir IPs de rede local (192.168.x.x, 10.x.x.x, 172.16-31.x.x)
                if (System.Net.IPAddress.TryParse(host, out var ip))
                {
                    var bytes = ip.GetAddressBytes();
                    
                    // Rede Classe A: 10.0.0.0/8
                    if (bytes[0] == 10)
                        return true;
                    
                    // Rede Classe B: 172.16.0.0/12
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                        return true;
                    
                    // Rede Classe C: 192.168.0.0/16
                    if (bytes[0] == 192 && bytes[1] == 168)
                        return true;
                }
            }
            
            return false;
        })
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Comentado para aceitar HTTP
// app.UseHttpsRedirection();

// ✅ IMPORTANTE: CORS deve vir ANTES de MapControllers
app.UseCors("AllowBlazor");

// Mapear controllers
app.MapControllers();

await app.RunAsync();