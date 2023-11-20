using System.Security.Cryptography.X509Certificates;
using CoreServer.Application;
using CoreServer.Infrastructure;
using CoreServer.Infrastructure.Persistence;
using CoreServer.Infrastructure.RPC;
using CoreServer.Infrastructure.Unity;
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
    options.AddPolicy("prod", builder =>
    {
        builder.WithOrigins("https://localhost:44447", "https://localhost:4200", hostName ?? "") // the Angular app url
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
    options.AddPolicy("develop",
        x =>
        {
            x.AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed((host) => true)
                .AllowCredentials();
        });
});
WebApplication app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors("develop");
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseHsts();
    app.UseCors("prod");
}

// Initialise and seed database
using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDbContextInitialiser initialiser =
        scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
    await initialiser.InitialiseAsync();
    await initialiser.SeedAsync();
}


app.UseHealthChecks("/health");
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true, });

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
app.MapHub<UnityBrokerHub>("/api/unityBrokerHub");
app.MapFallbackToFile("index.html");

app.Run();