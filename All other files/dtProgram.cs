//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="ThingTrax UK Ltd">
// Copyright (c) ThingTrax Ltd. All rights reserved.
// </copyright>
// <summary>Web job program class.</summary>
//-----------------------------------------------------------------------

namespace TT.Core.Downtime.Webjob
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Net;
    using System.Threading;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using Serilog.Events;
    using StackExchange.Redis;
    using TT.Core.Api.AwsSecretsManager;
    using TT.Core.Models.Configurations;
    using TT.Core.Models.Enums;
    using TT.Core.Repository;
    using TT.Core.Repository.Sql;
    using TT.Core.Services;
    using TT.Core.Services.BackgroungAppServices;

    /// <summary>
    /// Web job program class.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Gets or sets the service provider.
        /// </summary>
        public static IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Gets or sets the configuration root.
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether email errors is turned on or not.
        /// </summary>
        public static bool EmailErrors { get; set; }

        /// <summary>
        /// Gets or sets the environment.
        /// </summary>
        /// <value>
        /// The environment.
        /// </value>
        public static string CustomerEnvironment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is downtime included.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is downtime included; otherwise, <c>false</c>.
        /// </value>
        public static bool ISDowntimeIncluded { get; set; }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("CORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                              .SetBasePath(Directory.GetCurrentDirectory())
                              .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                              .AddEnvironmentVariables();

            var webApplicationOptions = new WebApplicationOptions
            {
                EnvironmentName = environmentName,
                Args = args,
            };

            var webBuilder = WebApplication.CreateBuilder(webApplicationOptions);
            CustomerEnvironment = environmentName;
            ////Configuration = builder.Build();
            Configuration = webBuilder.Configuration;
            if (!string.Equals(environmentName, "development", StringComparison.InvariantCultureIgnoreCase))
            {
                webBuilder.Host.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddAmazonSecretsManager(
                        Configuration["awsSeceret:region"], Configuration["awsSeceret:secretId"], Configuration["awsSeceret:versionStage"], Configuration["awsSeceret:accessKey"], Configuration["awsSeceret:secretKey"]);
                });
            }

            ProjectTypes downtime = ProjectTypes.Downtime;
            ApplicationLogger.ConfigureLogger(Configuration, environmentName, downtime);

            var redisConnectionString = $"{Configuration["redisURL"]},abortConnect=False,allowAdmin=true";
            var hostbuilder = new HostBuilder()
                   .UseEnvironment(environmentName)
                   .ConfigureHostConfiguration(c =>
                   {
                       c.AddConfiguration(Configuration);
                   })
                   .ConfigureServices((hostContext, services) =>
                   {
                       services.AddMemoryCache();
                       services.AddHostedService<DowntimeBackgroundService>();
                       services.AddOptions();
                       services.Configure<EnergySettings>(Configuration.GetSection("EnergySettings"));
                       services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));
                       services.Configure<JobSettings>(Configuration.GetSection("JobSettings"));
                       services.Configure<HealthCheckSettings>(Configuration.GetSection("HealthCheckSettings"));
                       services.Configure<TokenConfig>(Configuration.GetSection("AzureAd"));
                       services.Configure<AzureEmailSenderOptions>(Configuration.GetSection("AzureEmailSenderOptions"));
                       services.AddSingleton(provider => Configuration);
                       services.AddLogging(builder => { builder.AddSerilog(); });
                       services.RegisterDependencyForServices();
                       services.RegisterDependencyForStoreProcedure();
                       services.RegisterDependencyForRepository();
                       services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
                       services.AddHttpContextAccessor();

                       services.AddDbContext<CoreContext>(
                                                           opt => opt.UseSqlServer($"{Configuration["sqlConnectionString"]}", o => o.CommandTimeout(180))
                                                                     .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking), ServiceLifetime.Transient);

                       services.AddDbContext<TelemetryContext>(
                                               opt => opt.UseSqlServer($"{Configuration["telemetryConnectionString"]}", o => o.CommandTimeout(180))
                                                         .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking), ServiceLifetime.Transient);

                       services.AddSignalR();
                   })
                   .UseConsoleLifetime();

            var host = hostbuilder.Build();
            using (host)
            {
                host.RunAsync().Wait();
            }
        }
    }
}
