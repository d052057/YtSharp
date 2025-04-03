using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.Json.Serialization;
using YtSharp.Server.Hubs;
using YtSharp.Server.Service;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
// Add services to the container.
builder.Services.AddCors();

builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddDirectoryBrowser();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IDownloadService, DownloadService>();
builder.Services.AddControllers().AddNewtonsoftJson(
               options =>
               {
                   options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                   options.SerializerSettings.TypeNameHandling = TypeNameHandling.None;
                   options.SerializerSettings.Formatting = Formatting.Indented;
               }
               )
               .AddXmlSerializerFormatters()
               .AddJsonOptions(options =>
               {
                   options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                   options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
               })
               .AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver()
            );

var app = builder.Build();

var corsUrls = builder.Configuration.GetSection("CorsUrls:AllowedOrigins").Get<string[]>();
if (corsUrls == null)
{
    throw new InvalidOperationException("CorsUrls:AllowedOrigins configuration section is missing or empty.");
}
app.UseCors(opt =>
{
    opt
    .WithOrigins(corsUrls)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    ;
});
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
var MimeProvider = new FileExtensionContentTypeProvider();
MimeProvider.Mappings[".flac"] = "audio/flac";
MimeProvider.Mappings[".flv"] = "video/x-flv";
MimeProvider.Mappings[".mkv"] = "video/mp4";
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(@"c:/medias"),
    RequestPath = "/medias",
    ContentTypeProvider = MimeProvider
});
app.UseDirectoryBrowser(new DirectoryBrowserOptions
{
    FileProvider = new PhysicalFileProvider(@"c:/medias"),
    RequestPath = "/medias"
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(
        options =>
        {
            options.SwaggerEndpoint("../openapi/v1.json", "version 1");
        });
}
app.UseRouting();

app.UseAuthorization();

app.MapControllers(); // Maps your API controllers
app.MapHub<DownloadHub>("/downloadHub"); // Maps your SignalR hub
app.MapFallbackToFile("/index.html");

app.Run();
