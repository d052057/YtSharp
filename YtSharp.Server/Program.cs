using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.Json.Serialization;
using YtSharp.Server.services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// Add services to the container.
builder.Services.AddDirectoryBrowser();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
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
app.UseSession();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
