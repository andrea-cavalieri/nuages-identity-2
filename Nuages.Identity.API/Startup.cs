﻿using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using AspNetCore.Identity.Mongo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Nuages.Identity.API.Services;
using Nuages.Identity.Services;
using Nuages.Identity.Services.AspNetIdentity;
using Nuages.Localization;
using Nuages.Web;
using OpenIddict.Abstractions;

namespace Nuages.Identity.API;

[ExcludeFromCodeCoverage]
public class Startup
{
    private readonly IWebHostEnvironment _env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        _env = env;
    }

    private IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDataProtection()
            .PersistKeysToAWSSystemsManager($"{Configuration["Nuages:IdentityAPI:StackName"]}/DataProtection");
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        services.AddHttpClient();
        services.AddEndpointsApiExplorer();
        
        services.AddSingleton(Configuration);
            
        services.AddHttpContextAccessor();
            
        if (!_env.IsDevelopment())
        {
            AWSXRayRecorder.InitializeInstance(Configuration);
            AWSXRayRecorder.RegisterLogger(LoggingOptions.Console);
            AWSSDKHandler.RegisterXRayForAllServices();
        }
        
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }).AddNuagesLocalization(Configuration);

        // services
        //     .AddIdentityCore<NuagesApplicationUser>()
        //     .AddRoles<NuagesApplicationRole>()
        //     .AddMongoDbStores<NuagesApplicationUser, NuagesApplicationRole, string>(mongo =>
        //     {
        //         mongo.ConnectionString =
        //             Configuration["Nuages:Mongo:ConnectionString"];
        //     })
        //     .AddDefaultTokenProviders()
        //     .AddNuagesIdentityAPI(Configuration);;
        //
        
         var identityOptions = Configuration.GetSection("Nuages:Identity").Get<NuagesIdentityOptions>();
        
        services.AddIdentityMongoDbProvider<NuagesApplicationUser, NuagesApplicationRole, string>(identity =>
            {
                identity.Lockout = new LockoutOptions
                {
                    AllowedForNewUsers = identityOptions.SupportsUserLockout,
                    MaxFailedAccessAttempts = 5,
                    DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5)
                };

                identity.Stores = new StoreOptions
                {
                    MaxLengthForKeys = 0,
                    ProtectPersonalData = false
                };

                identity.Tokens = new TokenOptions
                {
                    ProviderMap = new Dictionary<string, TokenProviderDescriptor>(),
                    EmailConfirmationTokenProvider = TokenOptions.DefaultProvider,
                    PasswordResetTokenProvider = TokenOptions.DefaultProvider,
                    ChangeEmailTokenProvider = TokenOptions.DefaultProvider,
                    ChangePhoneNumberTokenProvider = TokenOptions.DefaultPhoneProvider,
                    AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider,
                    AuthenticatorIssuer =  "Microsoft.AspNetCore.Identity.UI"
                };

                identity.Password = identityOptions.Password;

                identity.User = new UserOptions
                {
                    AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+",
                    RequireUniqueEmail = true /* Not the default*/
                };

                identity.ClaimsIdentity = new ClaimsIdentityOptions
                {
                    RoleClaimType = OpenIddictConstants.Claims.Role,
                    UserNameClaimType = OpenIddictConstants.Claims.Name,
                    UserIdClaimType = OpenIddictConstants.Claims.Subject,
                    EmailClaimType = ClaimTypes.Email,
                    SecurityStampClaimType =  "AspNet.Identity.SecurityStamp"
                };

                identity.SignIn = new SignInOptions
                {
                    RequireConfirmedEmail = identityOptions.RequireConfirmedEmail,
                    RequireConfirmedPhoneNumber = identityOptions.RequireConfirmedPhoneNumber,
                    RequireConfirmedAccount = false
                };
                
            },
            mongo =>
            {
                var connection = Configuration["Nuages:Mongo:ConnectionString"];
                
                mongo.ConnectionString = connection;

            }).AddNuagesIdentityAPI(Configuration);
        
        
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer("Bearer", options =>
        {
            options.Authority = identityOptions.Authority;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                // ActorValidationParameters = null,
                // AlgorithmValidator = null,
                // AudienceValidator = null,
                // AuthenticationType = null,
                // ClockSkew = default,
                // CryptoProviderFactory = null,
                // IgnoreTrailingSlashWhenValidatingAudience = false,
                // IssuerSigningKeyValidator = null,
                // IssuerSigningKey = null,
                // IssuerSigningKeyResolver = null,
                // IssuerSigningKeys = null,
                // IssuerValidator = null,
                // LifetimeValidator = null,
                // NameClaimType = null,
                // NameClaimTypeRetriever = null,
                // PropertyBag = null,
                RequireAudience = true,
                // RequireExpirationTime = false,
                // RequireSignedTokens = false,
                // RoleClaimType = null,
                // RoleClaimTypeRetriever = null,
                // SaveSigninToken = false,
                // SignatureValidator = null,
                // TokenDecryptionKey = null,
                // TokenDecryptionKeyResolver = null,
                // TokenDecryptionKeys = null,
                // TokenReader = null,
                // TokenReplayCache = null,
                // TokenReplayValidator = null,
                // TryAllIssuerSigningKeys = false,
                // TypeValidator = null,
                // ValidateActor = false,
                ValidateAudience = true,
                ValidateIssuer = true,
                // ValidateIssuerSigningKey = false,
                // ValidateLifetime = false,
                // ValidateTokenReplay = false,
                // ValidAlgorithms = null,
                //ValidAudience = Configuration["Nuages:Identity:Audience"],
                ValidAudiences = identityOptions.Audiences,
                ValidIssuer = identityOptions.Authority,
                // ValidIssuers = null,
                // ValidTypes = null

            };
        });

        services.AddAuthorization(o => 
            o.AddPolicy("Admin",
                b =>
                {
                    b.RequireRole("Admin");
                    //b.RequireAuthenticatedUser();
                })
            );

        
        services.AddDataProtection()
            .PersistKeysToAWSSystemsManager($"{Configuration["Nuages:Sender:StackName"]}/DataProtection");
        
        services.AddHealthChecks();
        
        services.AddSwaggerDocument(config =>
        {
            config.PostProcess = document =>
            {
                document.Info.Version = "v1";
                document.Info.Title = "Nuages Sender Service";

                document.Info.Contact = new NSwag.OpenApiContact
                {
                    Name = "Nuages.io",
                    Email = string.Empty,
                    Url = "https://github.com/nuages-io/nuages-sender"
                };
                document.Info.License = new NSwag.OpenApiLicense
                {
                    Name = "Use under LICENCE",
                    Url = "http://www.apache.org/licenses/LICENSE-2.0"
                };
            };
        });
        
        // services.AddHttpLogging(logging =>
        // {
        //     logging.LoggingFields = HttpLoggingFields.ResponseBody;
        //     logging.ResponseBodyLogLimit = 4096;
        // });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        //app.UseHttpLogging();
        
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapCustomEndpoints(serviceProvider);
            endpoints.MapHealthChecks("/health");
        });
        
        app.UseOpenApi();
        app.UseSwaggerUi3();

    }
}