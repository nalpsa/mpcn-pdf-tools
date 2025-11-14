using PdfProcessor.Core.Interfaces;
using PdfProcessor.Infrastructure.PdfServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // IMPORTANTE: Adicionar controllers

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar serviços do PDF Processor
builder.Services.AddScoped<IPdfRotateService, PdfRotateService>();
//builder.Services.AddScoped<IPdfMergeService, PdfMergeService>(); // Futuro
//builder.Services.AddScoped<IPdfCompressService, PdfCompressService>(); // Futuro

// Configurar CORS para permitir chamadas do Blazor
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("https://localhost:5087", "http://localhost:5087") // Ajuste para porta do Blazor
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

app.UseHttpsRedirection();

// IMPORTANTE: Habilitar CORS
app.UseCors("AllowBlazor");

// IMPORTANTE: Mapear controllers
app.MapControllers();

await app.RunAsync();