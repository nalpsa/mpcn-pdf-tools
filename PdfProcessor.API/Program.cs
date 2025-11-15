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

// ✅ CORS ATUALIZADO: Aceita localhost E IP da rede
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins(
                "https://localhost:5087",
                "http://localhost:5087",
                "http://10.0.0.50:5087",     // ← Acesso pela rede
                "http://10.0.0.50:5239")     // ← API pela rede
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

// Habilitar CORS
app.UseCors("AllowBlazor");

// Mapear controllers
app.MapControllers();

await app.RunAsync();