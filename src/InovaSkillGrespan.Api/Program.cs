using InovaSkillGrespan.Api.Middleware;
using InovaSkillGrespan.Application.Imports;
using InovaSkillGrespan.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<ProcessImportFileUseCase>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseApiExceptionHandler();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
