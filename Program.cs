using Microsoft.EntityFrameworkCore;
using my_project.Application;
using my_project.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<TimeRegistrationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TimeRegistration")
        ?? "Data Source=timeregistration.db"));

builder.Services.AddSingleton(TimeProvider.System);

// No authentication story exists yet (spec 001 §10 Q1), so the current user is a dev-only stub
// driven by the person switcher in the layout. Swap this one registration when auth lands.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, DevUserSwitchCurrentUser>();

builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<PersonService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<TimesheetService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/StatusCode", "?code={0}");

// The services refuse non-managers themselves, not just the pages that host them (NFR-002). If one
// of them fires, that is a refusal to show the user — not a crash to report.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ForbiddenException)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }
});

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TimeRegistrationDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await DemoData.SeedAsync(db, app.Services.GetRequiredService<TimeProvider>());
    }
}

app.Run();
