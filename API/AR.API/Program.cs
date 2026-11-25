using System.Reflection;
using AR.API.Infrastructure.ConfigurationExtensions;
using AR.API.Infrastructure.ExceptionHandlers;
using Microsoft.AspNetCore.Http.Features;
using AR.API.Infrastructure.Middlewares;
using AR.Infrastructure.Hosting;

var assemblyName = $"{Assembly.GetExecutingAssembly().GetName().Name} ({AREnvironment.GetEnvironmentName()})";
Console.WriteLine($"'{assemblyName}': starting.");

var builder = WebApplication.CreateBuilder(args);

// get config for max request bytes
var maxRequestBodySizeBytes = builder.Configuration.GetValue<long>("AppSettings:MaxRequestBodySizeBytes");

// configure server web host
builder.WebHost
       .ConfigureKestrel(options =>
       {
           options.AddServerHeader = false;
           options.Limits.MaxRequestBodySize = maxRequestBodySizeBytes;
       });

// configure configs & logging
builder
    .ConfigureConfigurationExt()
    .ConfigureLoggingExt();

#region Configure Services

builder
    .Services
    .Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maxRequestBodySizeBytes)
    .AddExceptionHandlerExt()
    .ConfigureOptionsExt(builder.Configuration)
    // with DB context config
    .ConfigureDIModulesExt(builder.Configuration)
    // add api http context
    .AddHttpContextAccessor()
    // configure cors
    .ConfigureCorsExt(builder.Configuration)
    // add auth configurations
    .ConfigureAuthorizationExt(builder.Configuration);
    // // add azure config
    // .AddAzureAppConfiguration()

builder
    .Services
    .AddEndpointsApiExplorer()
    .AddControllers(options =>
    {
        options.RespectBrowserAcceptHeader = true;
        options.ReturnHttpNotAcceptable = true;
    })
    .ConfigureJsonOptionsExt();
    // add api versions
    builder.Services
           .ConfigureApiVersionsExt()
           .ConfigureOpenApiExt();

#endregion

#region Configure Application

Console.WriteLine($"'{assemblyName}': initializing AppConfiguration");

var app = builder.Build();

if (AREnvironment.IsLocal())
{
    app.UseDeveloperExceptionPage();
}

// enable custom middlewares
app
    .UseExceptionHandlerExt()
    .UseForwardedHeaders()
    .UseHsts()
    .UseHttpsRedirection()
    .UseRouting()
    .UseCorsExt()
    .UseCustomMiddlewaresBeforeAuthExt()
    .UseAuthentication()
    .UseAuthorization()
    .UseCustomMiddlewaresExt();

app.UseOpenApiExt();

app.MapGet("/ping", () => $"pong from {AREnvironment.GetEnvironmentName()}!").ExcludeFromDescription();
app.MapControllers();

Console.WriteLine($"'{assemblyName}': initialized AppConfiguration");

#endregion

// run app
app.Run();

Console.WriteLine($"'{assemblyName}.dll' started");
