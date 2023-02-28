using CreditCardApiSimple.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// conn str
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// secret key from appsettings.json
var key = builder.Configuration.GetValue<string>("AppSettings:Secret");

// Add services to the container.

builder.Services.AddControllers();

// add mysql service
builder.Services.AddDbContext<ApplicationDbContext>(
    Options => Options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// CORS
builder.Services.AddCors(p => p.AddPolicy("PolicyCors", build =>
{
    build.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));

// add authentication to swagger
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

//  add jwt to swagger
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.ToString());

    options.SwaggerDoc("users", new OpenApiInfo
    {
        Title = "Usuarios",
        Version = "v1",
    });

    options.SwaggerDoc("creditcard", new OpenApiInfo
    {
        Title = "Tarjeta de crédito",
        Version = "v1",
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticando JWT usando el esquema Bearer. \r\n\r\n " +
        "Ingresa la palabra 'Beare' seguida de un [espacio] y luego su token en el campo de abajo \r\n\r\n" +
        "Ej: \"Bearer tdadahsdh\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/users/swagger.json", "users");
        options.SwaggerEndpoint("/swagger/creditcard/swagger.json", "creditcard");
    });
}

app.UseRouting();

// CORS
app.UseCors("PolicyCors");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
