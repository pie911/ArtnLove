using ArtnLove.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Bind Supabase options from configuration (placeholders in appsettings.*)
builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection("Supabase"));

// Add application services
builder.Services.AddSingleton<SupabaseService>();
builder.Services.AddSingleton<SupabaseAuthService>();
builder.Services.AddHttpClient();
// Upload options
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection("Uploads"));
builder.Services.AddSingleton<ArtnLove.Data.ArtRepository>();
// Image analysis service
builder.Services.AddSingleton<ArtnLove.Services.ImageAnalysisService>();
builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();

// CORS for local dev/frontend (tighten for production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDev", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowLocalDev");

// Apply Supabase JWT middleware for API routes to populate HttpContext.User from Supabase JWTs
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseMiddleware<ArtnLove.Middleware.SupabaseJwtMiddleware>();
});

app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

// API controllers
app.MapControllers();

// MVC route for existing views
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
