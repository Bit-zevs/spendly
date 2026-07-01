using Spendly.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices();

var app = builder.Build();

app.UseApiPipeline();
app.MapApiEndpoints();

app.Run();

public partial class Program;
