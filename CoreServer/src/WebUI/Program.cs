using System.Security.Cryptography.X509Certificates;
using CoreServer.Application;
using CoreServer.Infrastructure;
using CoreServer.Infrastructure.Persistence;
using CoreServer.Infrastructure.RPC;
using CoreServer.Infrastructure.Unity;
using WebUI;
using WebUI.Services;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


//run a task in 1 second to write hello world to the console
Task.Run(async () =>
{
    byte[] binaryData = File.ReadAllBytes("C:/ssl/certificate.pfx");

    // Convert the binary data to a base64 string
    string certificateBase64String = Convert.ToBase64String(binaryData);
    await Task.Delay(1000);
    var cert=new X509Certificate2("C:/ssl/certificate2.pfx");
        var bytes = cert.Export(X509ContentType.Cert);
        var base64 = Convert.ToBase64String(bytes);
        Console.WriteLine(base64);
    
});


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
app.UseCors("develop");
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
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    //add a cache control header to static files in webgl folder
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.Contains("webgl"))
        {
            var lastModified=ctx.File.LastModified;
            ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={(DateTimeOffset.UtcNow-lastModified).TotalSeconds}");
        }
    }
    
});

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