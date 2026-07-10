using StudyHelper.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddSessionStateTempDataProvider();  // Use server-side session for TempData security

// Add memory cache for performance
builder.Services.AddMemoryCache();

// Rate limiting: restrict login attempts to 5 per IP per minute to mitigate brute-force attacks
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    // Return 429 Too Many Requests when the limit is exceeded
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Name = "StudyHelper.Auth";
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Register authentication services
builder.Services.AddSingleton<IEncryptionService, EncryptionService>(); // US-005: re-enabled for users.dat encryption
builder.Services.AddScoped<IUserService, UserService>();

// Register study materials services
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IUserStudyMaterialService, UserStudyMaterialService>();

// Register quiz services
builder.Services.AddScoped<IMarkdownParserService, MarkdownParserService>();
builder.Services.AddScoped<IQuestionGeneratorService, QuestionGeneratorService>();

// Register exercise services
builder.Services.AddScoped<IEquationParserService, EquationParserService>();
builder.Services.AddScoped<IExerciseProblemGeneratorService, ExerciseProblemGeneratorService>();

// Register flashcard services
builder.Services.AddScoped<ITermDefinitionParserService, TermDefinitionParserService>();
builder.Services.AddScoped<IEquationFlashcardParserService, EquationFlashcardParserService>();

// Register graded quiz services
builder.Services.AddScoped<IGradedQuizService, GradedQuizService>();

// Register graded exercise services
builder.Services.AddScoped<IGradedExerciseService, GradedExerciseService>();

// Register Super Quiz services
builder.Services.AddScoped<ISuperQuizService, SuperQuizService>();

// Configure session for TempData
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;  // Prevent CSRF attacks
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
});

var app = builder.Build();

// Add security headers middleware
app.Use(async (context, next) =>
{
    // Content Security Policy
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' https://cdn.jsdelivr.net; " +  // Inline styles must be moved to external .css files
        "img-src 'self' data:; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none';");

    // Additional security headers
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Enable session middleware
app.UseSession();

app.UseRateLimiter();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
