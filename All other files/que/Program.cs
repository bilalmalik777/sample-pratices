//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="ThingTrax UK Ltd">
// Copyright (c) ThingTrax Ltd. All rights reserved.
// </copyright>
// <summary>Web job program class.</summary>
//-----------------------------------------------------------------------

namespace TT.Core.Telemetry.WebJob
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs.Primitives;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using MQTTnet;
    using MQTTnet.Client;
    using Serilog;
    using Serilog.Events;
    using TT.Core.Api.AwsSecretsManager;
    using TT.Core.Models.Configurations;
    using TT.Core.Models.Enums;
    using TT.Core.Repository;
    using TT.Core.Repository.Sql;
    using TT.Core.Services;

    /// <summary>
    /// Web job program class.
    /// </summary>
    public static class Program
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
        /// Gets or sets the subscriber.
        /// </summary>
        /// <value>
        /// The subscriber.
        /// </value>
        public static IMqttClient TelemetrySubscriber { get; set; }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The args.</param>
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Async Main.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>The task.</returns>
        public static async Task MainAsync(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("CORE_ENVIRONMENT");

            Console.WriteLine($"Environment name: {environmentName}");

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

            ServiceProvider ??= BuildServiceProvider();

            ProjectTypes telemetry = ProjectTypes.Telemetry;
            ApplicationLogger.ConfigureLogger(Configuration, environmentName, telemetry);

            bool.TryParse(Configuration["emailErrors"], out bool emailFlag);
            EmailErrors = emailFlag;

            Console.WriteLine(Directory.GetCurrentDirectory());

            var hostbuilder = new HostBuilder()
                 .UseEnvironment(environmentName)
                 .ConfigureHostConfiguration(c =>
                 {
                     c.AddConfiguration(Configuration);
                 })
                 .UseConsoleLifetime();

            bool.TryParse(Environment.GetEnvironmentVariable("IsDockerDeployment"), out bool isDockerDeployment);
            if (isDockerDeployment)
            {
                TelemetrySubscriber = new MqttFactory().CreateMqttClient();

                TelemetrySubscriber.ApplicationMessageReceivedAsync += new Func<MqttApplicationMessageReceivedEventArgs, Task>(TelemetryMessageHandler.HandleApplicationMessageReceivedAsync);

                Thread thread = new Thread(() => CheckConnection());
                thread.Start();
            }
            else
            {
                var storageConnectionString = $"{Configuration["dashboardStorageConnectionString"]}";
                var blobContainerName = $"{Configuration["telemetryBlobContainerName"]}";

                var eventHubsConnectionString = $"{Configuration["eventHubConnectionString"]}";
                var eventHubName = $"{Configuration["eventHubName"]}";
                var consumerGroup = $"{Configuration["telemetryConsumerGroup"]}";

                var blobContainerClient = new BlobContainerClient(
                    storageConnectionString,
                    blobContainerName);

                await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var maximumBatchSize = 256;

                EventProcessorOptions options = new EventProcessorOptions();
                options.PrefetchCount = 512;
                options.MaximumWaitTime = TimeSpan.FromSeconds(30);

                var processor = new EventProcessor(
                    blobContainerClient,
                    maximumBatchSize,
                    consumerGroup,
                    eventHubsConnectionString,
                    eventHubName);

                using var cancellationSource = new CancellationTokenSource();

                // Starting the processor does not block when starting; delay
                // until the cancellation token is signaled.
                try
                {
                    await processor.StartProcessingAsync(cancellationSource.Token);
                    await Task.Delay(Timeout.Infinite, cancellationSource.Token);
                }
                catch (TaskCanceledException ex)
                {
                    Log.Logger.Error($"TaskCanceledException {ex}");

                    string emailBody = $"Environment: {environmentName}" + Environment.NewLine +
                                        $"Time: {DateTime.UtcNow}" + Environment.NewLine +
                    $"Error: {ex} {Environment.NewLine}" +
                                        $"TaskCanceledException Exception {ex}";

                    EventProcessor.LogException(emailBody);
                }
                finally
                {
                    //// Stopping may take up to the length of time defined
                    //// as the TryTimeout configured for the processor;
                    //// By default, this is 60 seconds.
                    await processor.StopProcessingAsync();
                }
            }

            var host = hostbuilder.Build();
            using (host)
            {
                await host.RunAsync();
            }
        }

        private static void LogAndNotifyAboutException(Exception ex)
        {
            Console.WriteLine(ex.ToString());

            if (Program.EmailErrors)
            {
                string emailBody = $"Environment: {Program.CustomerEnvironment}" + Environment.NewLine +
                                    $"Time: {DateTime.UtcNow}" + Environment.NewLine +
                                    $"Error: {ex}";

                var emailSettings = new EmailSettings
                {
                    MailServer = Program.Configuration["EmailSettings:ErrorEmailSettings:MailServer"],
                    FromMail = Program.Configuration["EmailSettings:ErrorEmailSettings:FromMail"],
                    MailPort = int.Parse(Program.Configuration["EmailSettings:ErrorEmailSettings:MailPort"]),
                    Password = Program.Configuration["EmailSettings:ErrorEmailSettings:Password"],
                };

                var emailService = new EmailService(emailSettings);
            }
        }

        /// <summary>
        /// Checks the connection.
        /// </summary>
        private static void CheckConnection()
        {
            string username = Configuration["MosquittoTelemetrySettings:username"];
            string password = Configuration["MosquittoTelemetrySettings:password"];
            string mosquittoConnectionString = Configuration["MosquittoTelemetrySettings:mosquittoConnectionString"];
            string topic = Configuration["MosquittoTelemetrySettings:topic"];
            string clientId = Configuration["MosquittoTelemetrySettings:clientId"];

            while (true)
            {
                if (!TelemetrySubscriber.IsConnected)
                {
                    try
                    {
                        Console.WriteLine("Connection started");
                        Console.WriteLine(DateTime.UtcNow.ToString());
                        Console.WriteLine(mosquittoConnectionString + "  " + username + "  " + password + "  " + topic + "  " + clientId);
                        TelemetrySubscriber.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(mosquittoConnectionString).WithCredentials(username, password)
                             .WithCleanSession(true).WithClientId(clientId).Build()).Wait();
                        TelemetrySubscriber.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce).Wait();
                        Console.WriteLine("Connection Successed");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception in making connection with telemetry");
                        Log.Logger.Error("Exception in making connection with telemetry", ex.ToString);
                        Console.WriteLine(ex.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("Connection  is connected");
                    Console.WriteLine(DateTime.UtcNow.ToString());
                }

                Thread.Sleep(1000);
            }
        }

        private static IServiceProvider BuildServiceProvider()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(provider => Configuration);
            services.Configure<AzureEmailSenderOptions>(Configuration.GetSection("AzureEmailSenderOptions"));
            services.AddHttpContextAccessor();

            services.AddDbContext<CoreContext>(
                                               opt => opt.UseSqlServer($"{Program.Configuration["sqlConnectionString"]}", o => o.CommandTimeout(180))
                                                         .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking), ServiceLifetime.Transient);

            // Telemetry db context.
            services.AddDbContext<TelemetryContext>(
                                   opt => opt.UseSqlServer($"{Program.Configuration["telemetryConnectionString"]}", o => o.CommandTimeout(180))
                                             .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking), ServiceLifetime.Transient);
            services.AddSignalR();

            return services.BuildServiceProvider();
        }
    }
}