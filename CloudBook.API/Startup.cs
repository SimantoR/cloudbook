using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace CloudBook.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            string server_folder = Path.Combine(Directory.GetCurrentDirectory(), "server_files");
            if (!Directory.Exists(server_folder))
                Directory.CreateDirectory(server_folder);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC support
            services.AddMvc();

            // services.AddDataProtection()
            //     .PersistKeysToFileSystem(new DirectoryInfo(@"\\server\share\directory\"))
            //     .ProtectKeysWithCertificate(new X509Certificate2("certificate.pfx", "[password]"));

            // Clear default claim types
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services
                .AddAuthentication(options =>   // * Add Authenticaion using JWT
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(cfg =>    // * Add JWT Configuration
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = false;
                    cfg.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidIssuer = Configuration["Token:Issuer"],
                        ValidAudience = Configuration["Token:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Configuration["Token:Key"])),
                        ClockSkew = TimeSpan.Zero,
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        RequireSignedTokens = true
                    };
                });

            // * Add Identity Service
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = false;
            })
                .AddEntityFrameworkStores<Data.Database.IdentityStore>()
                .AddDefaultTokenProviders();

            //<-- Inject connection settings to the IdentityStore holding all db objects -->//
            services.AddDbContext<Data.Database.IdentityStore>(builder => builder.UseMySql(
                connectionString: Configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped(typeof(Data.TokenConfig));
            services.Configure<Data.TokenConfig>(Configuration.GetSection("Token"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            // Enable identity authentication system
            app.UseAuthentication();

            app.UseMvc();
        }
    }
}