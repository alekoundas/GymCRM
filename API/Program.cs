using API.AutoMapper;
using API.Filters;
using API.JsonConverter;
using Business.Repository;
using Business.Services;
using Core.Dtos;
using Core.Models;
using Core.System;
using DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var appSettings = builder.Configuration.GetSection("TokenSettings").Get<TokenSettings>() ?? default!;

// Add services to the container.
builder.Services
    .AddControllers(options =>
    {
        // Suppress automatic 400 response for invalid ModelState (Dto validation)
        //options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        //options.ModelValidatorProviders.Clear(); // Optional: Clears default validation providers

        // Add custom validation filter
        options.Filters.Add<ValidateModelFilter>();
    })
    .AddJsonOptions(x =>
    {
        // Serialize enums as strings in api responses (e.g. Role).
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Ignore omitted parameters on models to enable optional params (e.g. User update).
        x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        // Ignore circular references.
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // Configure DateTime handling to always use UTC with 'Z'
        x.JsonSerializerOptions.Converters.Add(new JsonDateTimeConverter());
        x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
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




// Configure ApiBehaviorOptions to customize invalid ModelState response
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true; // Disable default validation response
    options.InvalidModelStateResponseFactory = context =>
    {
        // Log ModelState for debugging
        Console.WriteLine($"ModelState.IsValid: {context.ModelState.IsValid}");
        foreach (var entry in context.ModelState)
        {
            Console.WriteLine($"Key: {entry.Key}, Errors: {string.Join(", ", entry.Value.Errors.Select(e => e.ErrorMessage))}");
        }

        string[] validationErrors = [];
        foreach (var error in context.ModelState)
        {
            foreach (var errorDetail in error.Value.Errors)
            {
                var fieldName = error.Key.StartsWith("$.") ? error.Key.Substring(2) : error.Key;
                validationErrors.Append(errorDetail.ErrorMessage);
            }
        }

        var response = new ApiResponse<object>().SetErrorResponse("error", validationErrors);
        return new BadRequestObjectResult(response);
    };
});


// DB context and factory.
builder.Services.AddDbContext<ApiDbContext>();
builder.Services.AddTransient<IDbContextFactory<ApiDbContext>, ApiDbContextFactory>();
builder.Services.AddScoped<ApiDbContextInitialiser>();

// AutoMapper.
//builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
//builder.Services.AddAutoMapper(config =>
//{

//    config.AddProfile<AutoMapperProfile>();
//    // Scan for profiles in the current assembly or specific assemblies
//    //config.AddMaps(builder.Environment.ContentRootPath); // Scans all assemblies in the project
//    // Or specify specific assemblies:
//    // config.AddMaps(typeof(Program).Assembly);
//    // config.AddMaps(AppDomain.CurrentDomain.GetAssemblies()); // If you need to scan all assemblies
//});

builder.Services.AddAutoMapper(config =>
{
    //config.ConstructServicesUsing(type => builder.Services.BuildServiceProvider().GetService(type));
    config.AddProfile<AutoMapperProfile>();
    //config.AddMaps(typeof(ValidateModelFilter).Assembly);
});










//builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(AutoMapperProfile).Assembly));
builder.Services.AddScoped<ValidateModelFilter>();
builder.Services.AddTransient<TrainGroupDateDtoDateTimeToDateOnlyResolver>();
builder.Services.AddTransient<TrainGroupDateDtoDateOnlyToDateTimeResolver>();
builder.Services.AddTransient<TrainGroupDateAddDtoDateTimeToDateOnlyResolver>();
builder.Services.AddTransient<TrainGroupDateAddDtoDateOnlyToDateTimeResolver>();



builder.Services.AddSingleton<ClaimsIdentity>();
builder.Services.AddSingleton(TimeProvider.System);

// Add services.
//builder.Services.AddTransient(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddSingleton(appSettings); // appsettings.json
builder.Services.AddScoped<IUserService, UserService>();



builder.Services.AddDataProtection().PersistKeysToDbContext<ApiDbContext>();


// Add Identity.
builder.Services.AddIdentityCore<User>(options =>
    {
        // User
        options.User.RequireUniqueEmail = true;

        // Password
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;

        // Lockout
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddRoleManager<RoleManager<IdentityRole<Guid>>>()
    .AddSignInManager()
    .AddEntityFrameworkStores<ApiDbContext>()
    .AddTokenProvider<DataProtectorTokenProvider<User>>("REFRESHTOKENPROVIDER");



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

    options.Events = new JwtBearerEvents
    {

        // Token Revocation
        //OnTokenValidated = async context =>
        //{
        //    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
        //    var userName = context.Principal?.FindFirst("UserName")?.Value;
        //    if (userName != null)
        //    {
        //        var user = await userManager.FindByNameAsync(userName);
        //        if (user == null || await userManager.IsLockedOutAsync(user))
        //        {
        //            context.Fail("User account is locked or does not exist.");
        //        }
        //        // Validate security stamp
        //        var securityStamp = context.Principal.FindFirst("securityStamp")?.Value;
        //        if (securityStamp != null && securityStamp != await userManager.GetSecurityStampAsync(user))
        //        {
        //            context.Fail("Security stamp is invalid.");
        //        }
        //    }
        //},
        //    OnAuthenticationFailed = context =>
        //    {
        //        if (context.Exception is SecurityTokenExpiredException)
        //        {
        //            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        //            context.Response.ContentType = "application/json";
        //            return context.Response.WriteAsync(
        //                System.Text.Json.JsonSerializer.Serialize(new
        //                {
        //                    error = "Unauthorized",
        //                    message = "Token has expired."
        //                }));
        //        }
        //        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        //        context.Response.ContentType = "application/json";
        //        return context.Response.WriteAsync(
        //            System.Text.Json.JsonSerializer.Serialize(new
        //            {
        //                error = "Unauthorized",
        //                message = context.Exception.Message
        //            }));
        //    },
        //    OnChallenge = context =>
        //    {
        //        // Ensure unauthorized requests return 401
        //        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        //        context.Response.ContentType = "application/json";
        //        return context.Response.WriteAsync(
        //            System.Text.Json.JsonSerializer.Serialize(new
        //            {
        //                error = "Unauthorized",
        //                message = "Authentication required."
        //            }));
        //    }
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
//var apiDbContext = app.Services.GetService<ApiDbContext>();
//if (apiDbContext != null)
//{
//    apiDbContext.RunMigrations();
//    await apiDbContext.TrySeedInitialData();
//}


// Run migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var initialiser = scope.ServiceProvider.GetRequiredService<ApiDbContextInitialiser>();
    await initialiser.RunMigrationsAsync();
    await initialiser.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // HTTPS.
    //app.UseHttpsRedirection();
}

app.Run();
