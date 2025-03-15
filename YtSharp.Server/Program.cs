using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.Json.Serialization;
using YtSharp.Server;
using YtSharp.Server.services;
var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddCors();

builder.Services.AddSignalR();
builder.Services.AddDirectoryBrowser();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IYtSharpService, YtSharpService>();
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
app.UseCors(opt =>
{
    opt.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
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
app.MapHub<DownloadHub>("/downloadHub");
app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
