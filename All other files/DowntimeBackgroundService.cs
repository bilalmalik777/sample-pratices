// <copyright file="DowntimeBackgroundService.cs" company="ThingTrax UK Ltd">
// Copyright (c) ThingTrax Ltd. All rights reserved.
// </copyright>

namespace TT.Core.Services.BackgroungAppServices
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using TT.Core.Models.Configurations;
    using TT.Core.Services.Interfaces;

    /// <summary>
    /// DowntimeBackgroundService.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Hosting.BackgroundService" />
    public class DowntimeBackgroundService : BackgroundService
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<DowntimeBackgroundService> logger;

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        private readonly IConfiguration configuration;

        /// <summary>
        /// The memory cache.
        /// </summary>
        private readonly IDowntimeService downtimeService;

        /// <summary>
        /// The line downtime service.
        /// </summary>
        private readonly ILineDowntimeService lineDowntimeService;

        /// <summary>
        /// The energy service.
        /// </summary>
        private readonly IEnergyService energyService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DowntimeBackgroundService" /> class.
        /// </summary>
        /// <param name="lineDowntimeService">The line downtime service.</param>
        /// <param name="downtimeService">The downtime service.</param>
        /// <param name="energyService">The energy service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="configuration">The configuration.</param>
        public DowntimeBackgroundService(
            ILineDowntimeService lineDowntimeService,
            IDowntimeService downtimeService,
            IEnergyService energyService,
            ILogger<DowntimeBackgroundService> logger,
            IConfiguration configuration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.energyService = energyService ?? throw new ArgumentNullException("energyService");
            this.downtimeService = downtimeService ?? throw new ArgumentNullException(nameof(downtimeService));
            this.lineDowntimeService = lineDowntimeService ?? throw new ArgumentNullException(nameof(lineDowntimeService));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Stops the asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The stopping token.</param>
        /// <returns>
        /// A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.
        /// </returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Downtime Hosted Service is stopping.");
            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>
        /// A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.
        /// </returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = int.TryParse(this.configuration["minimumDowntimeInMinutes"], out int minimumDowntimeInMinutes);
            _ = int.TryParse(this.configuration["downtimeEnergyConsumptionAlertInKWH"], out int downtimeEnergyConsumptionAlertInKWH);
            _ = int.TryParse(this.configuration["downtimeEnergyConsumptionAlertInMinutes"], out int downtimeEnergyConsumptionAlertInMinutes);
            _ = int.TryParse(this.configuration["EnergyAlertWhenMachineIsDown"], out int energyAlertWhenMachineIsDownInMinutes);
            _ = int.TryParse(this.configuration["skewDowntimeInMinutes"], out int skewDownTimeInMintues);
            _ = bool.TryParse(this.configuration["LineCheck"], out bool lineCheck);
            _ = bool.TryParse(this.configuration["emailErrors"], out bool isEmailEnabled);
            int delay = this.DelayTheThread();
            await Task.Delay(1000 * delay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    this.logger.LogInformation("Downtime Worker running at: {time}", DateTime.UtcNow);
                    var sw = Stopwatch.StartNew();
                    sw.Start();

                    skewDownTimeInMintues = skewDownTimeInMintues == 0 ? 5 : skewDownTimeInMintues;
                    await this.downtimeService.UploadDownTime(minimumDowntimeInMinutes, downtimeEnergyConsumptionAlertInKWH, downtimeEnergyConsumptionAlertInMinutes, energyAlertWhenMachineIsDownInMinutes, skewDownTimeInMintues);
                    if (lineCheck)
                    {
                        await this.energyService.CalculateLineDowntimeEnergy();
                        await this.lineDowntimeService.UploadLineDownTime();
                    }

                    this.logger.LogInformation($"Downtime Processing completed {sw.Elapsed.TotalSeconds}");
                    sw.Reset();
                    this.logger.LogInformation($"60 second delay at DateTime {DateTime.UtcNow}");
                    await Task.Delay(1000 * 60, stoppingToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogInformation(ex.ToString());
                    this.logger.LogError(ex.ToString());
                    this.LogException(ex, isEmailEnabled);
                }
            }
        }

        /// <summary>
        /// Delays the thread.
        /// </summary>
        /// <returns>int.</returns>
        private int DelayTheThread()
        {
            var timeOfDay = DateTime.UtcNow.TimeOfDay;
            var nextFullMintue = TimeSpan.FromMinutes(Math.Ceiling(timeOfDay.TotalMinutes));
            int delta = (int)(nextFullMintue - timeOfDay).TotalSeconds;

            if (delta <= 30)
            {
                this.logger.LogInformation($"greater then 30 delta {delta}");
                delta = 0;
            }
            else
            {
                this.logger.LogInformation($"less then 30 delta {delta}");
                delta -= 30;
            }

            return delta;
        }

        /// <summary>
        /// Logs the exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="emailFlag">if set to <c>true</c> [email flag].</param>
        private void LogException(Exception ex, bool emailFlag)
        {
            if (emailFlag)
            {
                try
                {
                    var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                    string emailBody = $"Environment: {environmentName}" + Environment.NewLine +
                                       $"Time: {DateTime.UtcNow}" + Environment.NewLine +
                                       $"Error: {ex}";

                    var emailSettings = new EmailSettings
                    {
                        MailServer = this.configuration["EmailSettings:MailServer"],
                        FromMail = this.configuration["EmailSettings:FromMail"],
                        MailPort = int.Parse(this.configuration["EmailSettings:MailPort"]),
                        Password = this.configuration["EmailSettings:Password"],
                    };

                    var emailService = new EmailService(emailSettings);
                    ////emailService.SendErrorEmail(null, "TT.Core.Downtime", "notificationgroup@thingtrax.com", "ThingTrax Support", $"Error in environment: {environmentName}. Downtime Service.", emailBody);
                    this.logger.LogError($"TT.Core.Downtime / ThingTrax Support / Error in environment: {environmentName}. Downtime Service.", ex.ToString());
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Error: {exception}");
                    this.logger.LogError($"Error: {exception}");
                }
            }
        }
    }
}
