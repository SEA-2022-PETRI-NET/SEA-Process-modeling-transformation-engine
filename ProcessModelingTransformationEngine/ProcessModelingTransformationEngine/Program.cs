using ProcessModelingTransformationEngine.Application;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<ValidateBpmnService, ValidateBpmnService>();
builder.Services.AddScoped<DcrToPetriNetTransformerService, DcrToPetriNetTransformerService>();
builder.Services.AddScoped<BpmnToPetriNetTransformerService, BpmnToPetriNetTransformerService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod()
    .AllowAnyOrigin());

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }