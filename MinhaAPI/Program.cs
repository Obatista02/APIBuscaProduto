using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinhaAPI.Context;
using MinhaAPI.DTOs.Mappings;
using MinhaAPI.Extensions;
using MinhaAPI.Filters;
using MinhaAPI.Logging;
using MinhaAPI.Models;
using MinhaAPI.MyRateLimitOptions;
using MinhaAPI.Repository;
using MinhaAPI.Repository.interfaces;
using MinhaAPI.Services;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(ApiExceptionFilters));
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
}).AddNewtonsoftJson();

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "apicatalogo", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer JWT ",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                          {
                              Reference = new OpenApiReference
                              {
                                  Type = ReferenceType.SecurityScheme,
                                  Id = "Bearer"
                              }
                          },
                         new string[] {}
                    }
                });
});

builder.Logging.AddProvider(new CustomLoggerProvider(new CustomLoggerProviderConfiguration
{
    LogLevel = LogLevel.Information
}));

// Conectando a classe Context com o BD
string mySqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>options.UseMySql(mySqlConnection, ServerVersion.AutoDetect(mySqlConnection)));

var secretkey = builder.Configuration["JWT:SecretKey"] ?? throw new ArgumentException("Invalid secret key!!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        //obter chave secreta para gerar chave simetrica para assinat token
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretkey))
    };
});

var myOptions = new MyRateLimitOptions();

builder.Configuration.GetSection(MyRateLimitOptions.MyRateLimit).Bind(myOptions);

builder.Services.AddRateLimiter(rateLimiteoptions =>
{
    rateLimiteoptions.AddFixedWindowLimiter(policyName: "fixedwindow", options =>
    {
        options.PermitLimit = myOptions.PermitLimit;
        options.Window = TimeSpan.FromSeconds(myOptions.Window);
        options.QueueLimit = myOptions.QueueLimit;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;

    });

    rateLimiteoptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpcontext =>
                            RateLimitPartition.GetFixedWindowLimiter(
                                partitionKey: httpcontext.User.Identity?.Name ?? 
                                httpcontext.Request.Headers.Host.ToString(),

                                
                                factory: partition  => new FixedWindowRateLimiterOptions
                                {
                                    AutoReplenishment = true,
                                    PermitLimit = 2,
                                    QueueLimit = 0,
                                    Window = TimeSpan.FromSeconds(10)
                                }
                                ));
});


builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IProdutosRepository, ProdutoRepository>();
builder.Services.AddScoped(typeof(IRepository<>),typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Logging.AddProvider(new CustomLoggerProvider(new CustomLoggerProviderConfiguration
{
    LogLevel = LogLevel.Information
}));

builder.Services.AddAutoMapper(typeof(ProdutoDTOMappingProfile));

builder.Services.AddAuthorization();
//builder.Services.AddAuthentication("Bearer").AddJwtBearer();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("Admin").RequireClaim("id", "Guilherme"));

    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));

    options.AddPolicy("ExclusiveOnly", policy => policy.RequireAssertion(context => context.User.HasClaim(claim => claim.Type == "id" &&
                                                                                                          claim.Value == "Macoratti") || 
                                                                                                          context.User.IsInRole("SuperAdmin")));

});

builder.Services.AddAuthentication().
                 AddBearerToken();

//var OrigensComAcessoPermitido = "_origensComAcessoPermitido";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy => {
            policy.WithOrigins("http://www.airequest.io").WithMethods("GET","POST").AllowAnyHeader();
            });
    
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();
app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
