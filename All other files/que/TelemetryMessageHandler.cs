// <copyright file="TelemetryMessageHandler.cs" company="ThingTrax UK Ltd">
// Copyright (c) ThingTrax Ltd. All rights reserved.
// </copyright>

namespace TT.Core.Telemetry.WebJob
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using MQTTnet;
    using MQTTnet.Client;
    using Newtonsoft.Json;
    using Serilog;
    using TT.Core.Models;
    using TT.Core.Models.Constants;
    using TT.Core.Models.Telemetry;
    using TT.Core.Repository;

    /// <summary>
    /// TelemetryMessageHandler.
    /// </summary>
    public static class TelemetryMessageHandler
    {
        /// <summary>
        /// Handles the application message received asynchronous.
        /// </summary>
        /// <param name="eventArgs">The <see cref="MqttApplicationMessageReceivedEventArgs"/> instance containing the event data.</param>
        /// <returns>the task.</returns>
        public static async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            var message = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    var telemetry = JsonConvert.DeserializeObject<EquipmentTelemetryModel>(message);
                    using (var serviceScope = Program.ServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                    using (var telemetryContext = serviceScope.ServiceProvider.GetService<TelemetryContext>())
                    {
                        Console.WriteLine(telemetry.Created);
                        Console.WriteLine($"{telemetry.DeviceId}  type  {telemetry.Type}");

                        if (telemetry.Type == TelemetryTypes.Pulse)
                        {
                            await TelemetryMessage.SavePulseTelemetry(telemetryContext, telemetry);
                        }
                        else
                        {
                            TelemetryMessage.SaveTelemetryDataToDb(telemetryContext, message, telemetry, null);
                            await telemetryContext.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Log.Logger.Error(ex.ToString());
            }
        }
    }
}
