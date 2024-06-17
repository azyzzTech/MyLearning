using hangfire.web.api.v1.Filters;
using Hangfire;
using Hangfire.MemoryStorage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Hangfire In-Memory Storage usage
builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseDefaultTypeSerializer()
    .UseMemoryStorage());

// Hangfire Job Filter
GlobalJobFilters.Filters.Add(new LogJobFilter());

// Adding Hangfire Server
builder.Services.AddHangfireServer();

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

app.UseAuthorization();

app.MapControllers();

// Background Job Creation
app.Services.GetRequiredService<IBackgroundJobClient>();


// Adding Hangifre Dashboard
//app.UseHangfireDashboard("/hangfire");

// Adding Hangifre Dashboard with Authorization
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new JobsAuthorizationFilter() }
});

app.Run();
