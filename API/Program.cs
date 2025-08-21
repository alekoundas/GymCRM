using Business.Repository;
using Business.Services;
using Core.Models;
using Core.System;
using DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var appSettings = builder.Configuration.GetSection("TokenSettings").Get<TokenSettings>() ?? default!;

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        // Serialize enums as strings in api responses (e.g. Role).
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Ignore omitted parameters on models to enable optional params (e.g. User update).
        x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        // Ignore circular references.
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Swagger/OpenAPI configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config =>
{
    config.SwaggerDoc("v1", new OpenApiInfo() { Title = "App Api", Version = "v1" });
    config.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    config.AddSecurityRequirement(
        new OpenApiSecurityRequirement{
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type=ReferenceType.SecurityScheme,
                        Id="Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

// DB context and factory.
builder.Services.AddDbContext<ApiDbContext>();
builder.Services.AddTransient<IDbContextFactory<ApiDbContext>, ApiDbContextFactory>();
builder.Services.AddSingleton<ApiDbContextInitialiser>();

// AutoMapper.
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


builder.Services.AddSingleton<ClaimsIdentity>();
builder.Services.AddSingleton(TimeProvider.System);

// Add services.
//builder.Services.AddTransient(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddSingleton(appSettings); // appsettings.json
builder.Services.AddSingleton<IUserService, UserService>();




builder.Services.AddDataProtection().PersistKeysToDbContext<ApiDbContext>();


// Add Identity.
builder.Services.AddIdentityCore<User>()
               .AddRoles<IdentityRole<Guid>>()
               .AddRoleManager<RoleManager<IdentityRole<Guid>>>()
               .AddSignInManager()
               .AddEntityFrameworkStores<ApiDbContext>()
               .AddTokenProvider<DataProtectorTokenProvider<User>>("REFRESHTOKENPROVIDER");



// Identity with Guid key type
//builder.Services.AddIdentityCore<User>(options =>
//{
//    options.User.RequireUniqueEmail = true;
//})
//.AddRoles<IdentityRole<Guid>>()
//.AddRoleManager<RoleManager<IdentityRole<Guid>>>()
//.AddSignInManager<SignInManager<User>>()
//.AddUserStore<UserStore<User, IdentityRole<Guid>, ApiDbContext, Guid>>()
//.AddRoleStore<RoleStore<IdentityRole<Guid>, ApiDbContext, Guid>>()
//.AddEntityFrameworkStores<ApiDbContext>()
//.AddTokenProvider<DataProtectorTokenProvider<User>>("REFRESHTOKENPROVIDER");

// Configure JWT Bearer token and refresh token.
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromSeconds(appSettings.RefreshTokenExpireSeconds);
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RequireExpirationTime = true,
        ValidIssuers = appSettings.Issuers,
        ValidAudiences = appSettings.Audiences,

        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.SecretKey)),
        ClockSkew = TimeSpan.FromSeconds(0)
    };
});



var app = builder.Build();






// Log all requests at the start of the pipeline
app.Use(async (context, next) =>
{
    Console.WriteLine($"[START] Request: {context.Request.Method} {context.Request.Path}");
    Console.WriteLine($"Origin: {context.Request.Headers["Origin"]}");
    Console.WriteLine($"Headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
    await next();
    Console.WriteLine($"[END] Response: {context.Response.StatusCode}");
    Console.WriteLine($"Response Headers: {string.Join(", ", context.Response.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
});

// Exception handling
//app.UseExceptionHandler(errorApp =>
//{
//    errorApp.Run(async context =>
//    {
//        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
//        if (exceptionHandlerPathFeature?.Error != null)
//        {
//            var exception = exceptionHandlerPathFeature.Error;
//            Console.WriteLine($"Error: {exception.Message}");
//            Console.WriteLine($"Stack Trace: {exception.StackTrace}");
//            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
//            context.Response.ContentType = "application/json";
//            await context.Response.WriteAsync(
//                System.Text.Json.JsonSerializer.Serialize(new
//                {
//                    error = "Internal Server Error",
//                    message = exception.Message
//                }));
//        }
//    });
//});










// NGINX reverse proxy.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// CORS.
app.UseCors(options =>
    options
        .WithOrigins(appSettings.Audiences.ToArray())
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("/index.html");


// Run migrations and seed initial data.
var apiDbContext = app.Services.GetService<ApiDbContext>();
if (apiDbContext != null)
{
    apiDbContext.RunMigrations();
    await apiDbContext.TrySeedInitialData();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // HTTPS.
    //app.UseHttpsRedirection();
}

app.Run();
