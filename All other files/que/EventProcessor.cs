//-----------------------------------------------------------------------
// <copyright file="EventProcessor.cs" company="ThingTrax UK Ltd">
// Copyright (c) ThingTrax Ltd. All rights reserved.
// </copyright>
// <summary>Web job program class.</summary>
//-----------------------------------------------------------------------

namespace TT.Core.Telemetry.WebJob
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Primitives;
    using Azure.Messaging.EventHubs.Processor;
    using Azure.Storage.Blobs;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Serilog;
    using TT.Core.Models.AssemblyVm;
    using TT.Core.Models.Configurations;
    using TT.Core.Models.Constants;
    using TT.Core.Models.Telemetry;
    using TT.Core.Repository;
    using TT.Core.Services;

    /// <summary>
    /// Event Processor.
    /// </summary>
    public class EventProcessor : PluggableCheckpointStoreEventProcessor<EventProcessorPartition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventProcessor"/> class.
        /// </summary>
        /// <param name="storageClient">The storage client.</param>
        /// <param name="eventBatchMaximumCount">The event batch maximum count.</param>
        /// <param name="consumerGroup">The consumer group.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="eventHubName">Name of the event hub.</param>
        /// <param name="clientOptions">The client options.</param>
        public EventProcessor(
            BlobContainerClient storageClient,
            int eventBatchMaximumCount,
            string consumerGroup,
            string connectionString,
            string eventHubName,
            EventProcessorOptions clientOptions = default)
                : base(
                    new BlobCheckpointStore(storageClient),
                    eventBatchMaximumCount,
                    consumerGroup,
                    connectionString,
                    eventHubName,
                    clientOptions)
        {
        }

        /// <summary>
        /// Logs the exception.
        /// </summary>
        /// <param name="emailBody">The email body.</param>
        public static void LogException(string emailBody)
        {
            var environmentName = Environment.GetEnvironmentVariable("CORE_ENVIRONMENT");
            Console.WriteLine(emailBody);
            if (Program.EmailErrors)
            {
                try
                {
                    var emailSettings = new EmailSettings
                    {
                        MailServer = Program.Configuration["EmailSettings:MailServer"],
                        FromMail = Program.Configuration["EmailSettings:FromMail"],
                        MailPort = int.Parse(Program.Configuration["EmailSettings:MailPort"]),
                        Password = Program.Configuration["EmailSettings:Password"],
                    };

                    var emailService = new EmailService(emailSettings);
                    emailService.SendErrorEmail(null, "TT.Core.Telemetry.Webjob", "notificationgroup@thingtrax.com", "ThingTrax Support", $"Error in environment: {environmentName}. Telemetry Service.", emailBody);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Error: {exception.ToString()}");
                }
            }
        }

        /// <summary>
        /// Performs the tasks needed to process a batch of events for a given partition as they are read from the Event Hubs service.
        /// </summary>
        /// <param name="events">The batch of events to be processed.</param>
        /// <param name="partition">The context of the partition from which the events were read.</param>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken">CancellationToken</see> instance to signal the request to cancel the processing.  This is most likely to occur when the processor is shutting down.</param>
        /// <remarks>
        /// The number of events in the <paramref name="events" /> batch may vary.  The batch will contain a number of events between zero and batch size that was
        /// requested when the processor was created, depending on the availability of events in the partition within the requested <see cref="P:Azure.Messaging.EventHubs.Primitives.EventProcessorOptions.MaximumWaitTime" />
        /// interval.
        /// When events are available in the prefetch queue, they will be used to form the batch as quickly as possible without waiting for additional events from the Event Hub partition
        /// to be read.  When no events are available in prefetch the processor will wait until at least one event is available or the requested <see cref="P:Azure.Messaging.EventHubs.Primitives.EventProcessorOptions.MaximumWaitTime" />
        /// has elapsed.
        /// If <see cref="P:Azure.Messaging.EventHubs.Primitives.EventProcessorOptions.MaximumWaitTime" /> is <c>null</c>, the event processor will continue reading from the Event Hub
        /// partition until a batch with at least one event could be formed and will not dispatch any empty batches to this method.
        /// Should an exception occur within the code for this method, the event processor will allow it to propagate up the stack without attempting to handle it in any way.
        /// On most hosts, this will fault the task responsible for partition processing, causing it to be restarted from the last checkpoint.  On some hosts, it may crash the process.
        /// Developers are strongly encouraged to take all exception scenarios into account and guard against them using try/catch blocks and other means as appropriate.
        /// It is not recommended that the state of the processor be managed directly from within this method; requesting to start or stop the processor may result in
        /// a deadlock scenario, especially if using the synchronous form of the call.
        /// </remarks>
        /// <returns>
        /// the task.
        /// </returns>
        protected async override Task OnProcessingEventBatchAsync(
            IEnumerable<EventData> events,
            EventProcessorPartition partition,
            CancellationToken cancellationToken)
        {
            EventData lastEvent = null;

            try
            {
                Console.WriteLine($"Received events for partition {partition.PartitionId}");

                var sw = Stopwatch.StartNew();
                sw.Start();
                using (var serviceScope = Program.ServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                using (var telemetryContext = serviceScope.ServiceProvider.GetService<TelemetryContext>())
                {
                    foreach (var message in events)
                    {
                        try
                        {
                            string telemetryData = message.EventBody.ToString();
                            var equipmentTelemetry = JsonConvert.DeserializeObject<EquipmentTelemetryModel>(telemetryData);
                            var assemblyTelemetry = JsonConvert.DeserializeObject<AssemblyLineModelVm>(telemetryData);

                            if (equipmentTelemetry.Type == TelemetryTypes.Pulse)
                            {
                                await TelemetryMessage.SavePulseTelemetry(telemetryContext, equipmentTelemetry);
                            }
                            else
                            {
                                TelemetryMessage.SaveTelemetryDataToDb(telemetryContext, telemetryData, equipmentTelemetry, assemblyTelemetry);
                                await telemetryContext.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            var environmentName = Environment.GetEnvironmentVariable("CORE_ENVIRONMENT");

                            string emailBody = $"Environment: {environmentName}" + Environment.NewLine +
                                                $"Time: {DateTime.UtcNow}" + Environment.NewLine +
                                                $"Error: {ex} {Environment.NewLine}" +
                                                $"OnProcessingEventBatchAsync Message {message.MessageId}  {message.EnqueuedTime}   {message.PartitionKey}   {message.SequenceNumber}   {message.EventBody?.ToString()}";
                            Log.Logger.Error(ex.ToString());
                        }

                        lastEvent = message;
                    }

                    if (lastEvent != null)
                    {
                        await this.UpdateCheckpointAsync(
                            partition.PartitionId,
                            lastEvent.Offset,
                            lastEvent.SequenceNumber,
                            cancellationToken)
                        .ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                // It is very important that you always guard against exceptions in
                // your handler code; the processor does not have enough
                // understanding of your code to determine the correct action to take.
                // Any exceptions from your handlers go uncaught by the processor and
                // will NOT be redirected to the error handler.
                //
                // In this case, the partition processing task will fault and be restarted
                // from the last recorded checkpoint.
                Console.WriteLine($"Exception while processing events: {ex}");

                var environmentName = Environment.GetEnvironmentVariable("CORE_ENVIRONMENT");

                string emailBody = $"Environment: {environmentName}" + Environment.NewLine +
                                    $"Time: {DateTime.UtcNow}" + Environment.NewLine +
                                    $"Error: {ex} {Environment.NewLine}" +
                                    $"OnProcessingEventBatchAsync Exception while processing events {partition.PartitionId}";
                Log.Logger.Error(ex.ToString());
            }
        }

        /// <summary>
        /// Performs the tasks needed when an unexpected exception occurs within the operation of the
        /// event processor infrastructure.
        /// </summary>
        /// <param name="exception">The exception that occurred during operation of the event processor.</param>
        /// <param name="partition">The context of the partition associated with the error, if any; otherwise, <c>null</c>.  This may only be initialized for members of <see cref="T:Azure.Messaging.EventHubs.Primitives.EventProcessorPartition" />, depending on the point at which the error occurred.</param>
        /// <param name="operationDescription">A short textual description of the operation during which the exception occurred; intended to be informational only.</param>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> instance to signal the request to cancel the processing.  This is most likely to occur when the processor is shutting down.</param>
        /// <returns>
        /// the task.
        /// </returns>
        /// <remarks>
        /// This error handler is invoked when there is an exception observed within the event processor itself; it is not invoked for exceptions in
        /// code that has been implemented to process events or other overrides and extension points that are not critical to the processor's operation.
        /// The event processor will make every effort to recover from exceptions and continue processing.  Should an exception that cannot be recovered
        /// from be encountered, the processor will attempt to forfeit ownership of all partitions that it was processing so that work may be redistributed.
        /// The exceptions surfaced to this method may be fatal or non-fatal; because the processor may not be able to accurately predict whether an
        /// exception was fatal or whether its state was corrupted, this method has responsibility for making the determination as to whether processing
        /// should be terminated or restarted.  If desired, this can be done safely by calling <see cref="M:Azure.Messaging.EventHubs.Primitives.EventProcessor`1.StopProcessingAsync(System.Threading.CancellationToken)" /> and/or <see cref="M:Azure.Messaging.EventHubs.Primitives.EventProcessor`1.StartProcessingAsync(System.Threading.CancellationToken)" />.
        /// It is recommended that, for production scenarios, the decision be made by considering observations made by this error handler, the method invoked
        /// when initializing processing for a partition, and the method invoked when processing for a partition is stopped.  Many developers will also include
        /// data from their monitoring platforms in this decision as well.
        /// As with event processing, should an exception occur in the code for the error handler, the event processor will allow it to bubble and will not attempt to handle
        /// it in any way.  Developers are strongly encouraged to take exception scenarios into account and guard against them using try/catch blocks and other means as appropriate.
        /// </remarks>
        /// <seealso href="https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs/TROUBLESHOOTING.md">Troubleshoot Event Hubs issues</seealso>
        protected override Task OnProcessingErrorAsync(
            Exception exception,
            EventProcessorPartition partition,
            string operationDescription,
            CancellationToken cancellationToken)
        {
            var environmentName = Environment.GetEnvironmentVariable("CORE_ENVIRONMENT");
            try
            {
                if (partition != null)
                {
                    Console.Error.WriteLine(
                        $"Exception on partition {partition.PartitionId} while " +
                        $"performing {operationDescription}: {exception}");

                    string emailBody = $"Environment: {environmentName}" + Environment.NewLine +
                                       $"Time: {DateTime.UtcNow}" + Environment.NewLine +
                                       $"Error: {exception} {Environment.NewLine}" +
                                       $"Exception on partition {partition.PartitionId} while " +
                                       $"OnProcessingErrorAsync performing {operationDescription}";

                    Log.Logger.Error(emailBody);
                }
                else
                {
                    Console.Error.WriteLine(
                        $"Exception while performing {operationDescription}: {exception}");

                    string emailBody = $"Environment: {environmentName}" + Environment.NewLine +
                                       $"Time: {DateTime.UtcNow}" + Environment.NewLine +
                                       $"Error: {exception} {Environment.NewLine}" +
                                       $"OnProcessingErrorAsync Exception while performing {operationDescription}";

                    Log.Logger.Error(emailBody);
                }
            }
            catch (Exception ex)
            {
                // It is very important that you always guard against exceptions
                // in your handler code; the processor does not have enough
                // understanding of your code to determine the correct action to
                // take.  Any exceptions from your handlers go uncaught by the
                // processor and will NOT be handled in any way.
                //
                // In this case, unhandled exceptions will not impact the processor
                // operation but will go unobserved, hiding potential application problems.
                Console.WriteLine($"Exception while processing events: {ex}");

                string emailBody = $"Environment: {environmentName}" + Environment.NewLine +
                                   $"Time: {DateTime.UtcNow}" + Environment.NewLine +
                                   $"Error: {exception} {Environment.NewLine}" +
                                   $"OnProcessingErrorAsync Exception while processing events {operationDescription}";
                Log.Logger.Error(ex.ToString());
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs the tasks to initialize a partition, and its associated context, for event processing.
        /// </summary>
        /// <param name="partition">The context of the partition being initialized.  Only the well-known members of the <see cref="T:Azure.Messaging.EventHubs.Primitives.EventProcessorPartition" /> will be populated.  If a custom context is being used, the implementor of this method is responsible for initializing custom members.</param>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> instance to signal the request to cancel the initialization.  This is most likely to occur if the partition is claimed by another event processor instance or the processor is shutting down.</param>
        /// <returns>
        /// the task.
        /// </returns>
        /// <remarks>
        /// It is not recommended that the state of the processor be managed directly from within this method; requesting to start or stop the processor may result in
        /// a deadlock scenario, especially if using the synchronous form of the call.
        /// </remarks>
        protected override Task OnInitializingPartitionAsync(
            EventProcessorPartition partition,
            CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"Initializing partition {partition.PartitionId}");
            }
            catch (Exception ex)
            {
                // It is very important that you always guard against exceptions in
                // your handler code; the processor does not have enough
                // understanding of your code to determine the correct action to take.
                // Any exceptions from your handlers go uncaught by the processor and
                // will NOT be redirected to the error handler.
                //
                // In this case, the partition processing task will fault and the
                // partition will be initialized again.
                Console.WriteLine($"Exception while initializing a partition: {ex}");

                var environmentName = Environment.GetEnvironmentVariable("CORE_ENVIRONMENT");
                string emailBody = $"Environment: {environmentName}" + Environment.NewLine +
                                   $"Time: {DateTime.UtcNow}" + Environment.NewLine +
                                   $"Error: {ex} {Environment.NewLine}" +
                                   $"OnInitializingPartitionAsync Exception while processing events {partition?.PartitionId}";
                Log.Logger.Error(ex.ToString());
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs the tasks needed when processing for a partition is being stopped.  This commonly occurs when the partition
        /// is claimed by another event processor instance or when the current event processor instance is shutting down.
        /// </summary>
        /// <param name="partition">The context of the partition for which processing is being stopped.</param>
        /// <param name="reason">The reason that processing is being stopped for the partition.</param>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> instance to signal the request to cancel the processing.  This is not expected to signal under normal circumstances and will only occur if the processor encounters an unrecoverable error.</param>
        /// <returns>
        /// the task.
        /// </returns>
        /// <remarks>
        /// It is not recommended that the state of the processor be managed directly from within this method; requesting to start or stop the processor may result in
        /// a deadlock scenario, especially if using the synchronous form of the call.
        /// </remarks>
        protected override Task OnPartitionProcessingStoppedAsync(
            EventProcessorPartition partition,
            ProcessingStoppedReason reason,
            CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine(
                    $"No longer processing partition {partition.PartitionId} " +
                    $"because {reason}");
            }
            catch (Exception ex)
            {
                // It is very important that you always guard against exceptions in
                // your handler code; the processor does not have enough
                // understanding of your code to determine the correct action to take.
                // Any exceptions from your handlers go uncaught by the processor and
                // will NOT be redirected to the error handler.
                //
                // In this case, unhandled exceptions will not impact the processor
                // operation but will go unobserved, hiding potential application problems.
                Console.WriteLine($"Exception while stopping processing for a partition: {ex}");

                var environmentName = Environment.GetEnvironmentVariable("CORE_ENVIRONMENT");
                string emailBody = $"Environment: {environmentName}" + Environment.NewLine +
                                   $"Time: {DateTime.UtcNow}" + Environment.NewLine +
                                   $"Error: {ex} {Environment.NewLine}" +
                                   $"OnPartitionProcessingStoppedAsync No longer processing partition {partition?.PartitionId} Reason {reason}";
                Log.Logger.Error(ex.ToString());
            }

            return Task.CompletedTask;
        }
    }
}