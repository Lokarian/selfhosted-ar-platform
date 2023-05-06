using CoreServer.Application;
using CoreServer.Infrastructure;
using CoreServer.Infrastructure.Persistence;
using CoreServer.Infrastructure.RPC;
using WebUI;
using WebUI.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebUIServices(builder.Configuration);
string? hostName = Environment.GetEnvironmentVariable("HOST_NAME");
builder.Services.AddCors(options =>
{
    options.AddPolicy("test", builder =>
    {
        builder.WithOrigins("https://localhost:44447", "https://localhost:4200",hostName??"") // the Angular app url
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
WebApplication app = builder.Build();
app.UseCors("test");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
    // Initialise and seed database
    using (IServiceScope scope = app.Services.CreateScope())
    {
        ApplicationDbContextInitialiser initialiser =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHealthChecks("/health");
if(!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSwaggerUi3(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserMiddleware>();

app.MapControllerRoute(
    "default",
    "{controller}/{action=Index}/{id?}");

app.MapHub<SignalRHub>("/api/hub");
app.MapFallbackToFile("index.html");

app.Run();