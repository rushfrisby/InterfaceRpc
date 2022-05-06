using Carter.ModelBinding;
using InterfaceRpcDemoServiceSG;
using InterfaceRpcDemoSharedSG;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddTransient<IDemoService, DemoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapPost("/api/Whatever", async context =>
{
    var service = app.Services.GetService<IDemoService>();
    var o = await context.Request.Bind<string>();
    service.Echo(o);
});

app.UseAuthorization();

app.MapRazorPages();

app.Run();


