using Core_APIOracle.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddDbContext<DatabaseContext>(options =>
{
    var MyConfig = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
    var databaseType = MyConfig.GetValue<string>("Settings:DatabaseType");
    var AppName = MyConfig.GetValue<string>("Settings:APP_Name");
    if (databaseType.Contains("ORACLE") || databaseType.Contains("Oracle") || databaseType.Contains("oracle"))
    {
        options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection"));
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection"));
    }
});
// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
