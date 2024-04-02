using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Oblak.Data;
using Oblak.Filters;
using Oblak.Schedulers;
using Oblak.Interfaces;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;
using Oblak.SignalR;
using RB90;
using SendGrid.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder.Extensions;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Oblak.Services.FCM;
using Serilog;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Oblak.Services.Reporting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddAuthentication(x =>
{
    x.DefaultScheme = IdentityConstants.ApplicationScheme;
})
//.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(IdentityConstants.ApplicationScheme);

builder.Services.AddIdentityCore<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddSignInManager<Oblak.Identity.SignInManager<IdentityUser>>()
    .AddRoleManager<RoleManager<IdentityRole>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
;

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;    
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;    

    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

    options.User.RequireUniqueEmail = true;    

    options.SignIn.RequireConfirmedEmail = false;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/greska/pristup-zabranjen";
    options.LoginPath = "/";
});


/*
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Custom";
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
.AddPolicyScheme("Custom", "Custom", options =>
{
    options.ForwardDefaultSelector = context =>
    {        
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.InvariantCulture))
            return JwtBearerDefaults.AuthenticationScheme;
        else
            return IdentityConstants.ApplicationScheme;
    };
})
.AddCookie(IdentityConstants.ApplicationScheme)
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
    options.Authority = builder.Configuration["JWT:Issuer"];
    options.Audience = builder.Configuration["JWT:Audience"];    
    // Do not use in production.
    options.RequireHttpsMetadata = false;
});

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddSignInManager<SignInManager<ApplicationUser>>()
    .AddRoleManager<RoleManager<IdentityRole>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;

    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

    options.User.RequireUniqueEmail = true;
});

*/

// Add framework services.
builder.Services
    .AddControllersWithViews()
    .AddRazorRuntimeCompilation()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());    
    });

builder.Services.AddLocalization();
builder.Services.AddKendo();
builder.Services.AddSignalR();
//builder.Services.AddScoped<IHubContext>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options => {
    options.AddPolicy("CORSPolicy", builder => builder.AllowAnyMethod().AllowAnyHeader().AllowCredentials().SetIsOriginAllowed((hosts) => true));
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.IsEssential = true;
});

builder.Services.AddSendGrid(options =>
{
    options.ApiKey = builder.Configuration["SendGrid:ApiKey"];
});

builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection")));

builder.Services.AddHangfireServer();

//builder.Services.AddTransient<RB90Scheduler>();
builder.Services.AddTransient<SelfRegisterService>();
builder.Services.AddTransient<eMailService>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddScoped<mup, mupClient>();
//builder.Services.AddScoped<IRegister, Rb90Client>();
//builder.Services.AddScoped<IRegister, SrbClient>();
builder.Services.AddScoped<MneClient>();
builder.Services.AddScoped<SrbClient>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<EfiClient>();

builder.Services.AddTransient<SrbScheduler>();
builder.Services.AddTransient<FcmService>();

/*
builder.Services.AddScoped(provider => new rb90Client(
     logger: provider.GetRequiredService<ILogger<rb90Client>>(),
     contextAccesor: provider.GetRequiredService<IHttpContextAccessor>(),
     rb90client: provider.GetRequiredService<mup>(),
     db: provider.GetRequiredService<ApplicationDbContext>(),
     rb90GuestToken: provider.GetRequiredService<rb90GuestToken>(),
     configuration: provider.GetRequiredService<IConfiguration>(),
     eMailService: provider.GetRequiredService<eMailService>(), 
     webHostEnvironment: provider.GetRequiredService<IWebHostEnvironment>(),
     user: "not_set")
);
*/


var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");    
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (app.Environment.IsDevelopment())
    {
        if (builder.Configuration["RebuildDb"] == "true")
        {
            dataContext.Database.EnsureDeleted();
            dataContext.Database.Migrate();
        }
        else
        { 
            dataContext.Database.Migrate();
        }
    }
    else
    {
        dataContext.Database.Migrate();
    }
}

var supportedCultures = new[] { new CultureInfo("sr-Latn-ME")/*, new CultureInfo("en-US")*/ };

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("sr-Latn-ME"),    
    SupportedCultures = supportedCultures,    
    SupportedUICultures = supportedCultures,
    FallBackToParentCultures = false
});

CultureInfo.CurrentCulture = new CultureInfo("sr-Latn-ME");
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CreateSpecificCulture("sr-Latn-ME");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CreateSpecificCulture("sr-Latn-ME");

app.UseCors("CORSPolicy");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSerilogRequestLogging();

app.UseSession();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseHangfireDashboard("/dashboard", new DashboardOptions
{
	Authorization = new[] { new DashboardAuthFilter() },
    IgnoreAntiforgeryToken = true
});

app.MapHangfireDashboard();



if (!app.Environment.IsDevelopment())
{
    RecurringJob.AddOrUpdate<SrbScheduler>("HourlyCheckOutSrb", a => a.HourlyCheckOut(), builder.Configuration["SRB:Schedulers:HourlyCheckOut"]);
}

RecurringJob.AddOrUpdate<SrbScheduler>("HourlyCheckOutSrb", a => a.HourlyCheckOut(), builder.Configuration["SRB:Schedulers:HourlyCheckOut"]);

app.MapHub<MessageHub>("/messageHub");

Telerik.Windows.Documents.Extensibility.FixedExtensibilityManager.FontsProvider = new Oblak.Helpers.TelerikFontsProvider();
Telerik.Windows.Documents.Extensibility.FixedExtensibilityManager.JpegImageConverter = new Telerik.Documents.ImageUtils.JpegImageConverter();

FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FCM.json")),
});

app.Run();
