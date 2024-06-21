using middleware.web.api.v1.RateLimiters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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

// Fixed window Middleware Technique
//app.UseMiddleware<FixedWindow>(5, TimeSpan.FromMinutes(1));

// Sliding window Middleware Technique
//app.UseMiddleware<SlidingWindow>(5, TimeSpan.FromMinutes(1));

// Token Bucket Middleware Technique
//app.UseMiddleware<TokenBucket>(10, TimeSpan.FromSeconds(1));

// Leaky Bucket Middleware Technique
app.UseMiddleware<LeakyBucket>(10, TimeSpan.FromSeconds(1));

app.Run();
