// <copyright file="TelemetryMessage.cs" company="ThingTrax UK Ltd">
// Copyright (c) ThingTrax Ltd. All rights reserved.
// </copyright>

namespace TT.Core.Telemetry.WebJob
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using Serilog;
    using TT.Core.Models;
    using TT.Core.Models.AssemblyVm;
    using TT.Core.Models.Configurations;
    using TT.Core.Models.Constants;
    using TT.Core.Models.DeviceLog;
    using TT.Core.Models.Telemetry;
    using TT.Core.Repository;
    using TT.Core.Repository.Entities;
    using TT.Core.Services;
    using TT.Core.Services.Extensions;

    /// <summary>
    /// TelemetryMessage.
    /// </summary>
    public static class TelemetryMessage
    {
        /// <summary>
        /// Saves the telemetry data to database.
        /// </summary>
        /// <param name="telemetryContext">The telemetry context.</param>
        /// <param name="telemetry">The equipment telemetry.</param>
        /// <returns>the task.</returns>
        public static async Task SavePulseTelemetry(TelemetryContext telemetryContext, EquipmentTelemetryModel telemetry)
        {
            telemetry.Tags.Add(new TagModel { Type = "machine", Name = "cycletime", Label = "cycletime", Value = "{cycleTimeValue}" });
            var tlemetryJson = JsonConvert.SerializeObject(telemetry);
            var pulses = Convert.ToInt32(telemetry.Tags.FirstOrDefault(t => t.Name == "pulses")?.Value);
            var duration = Convert.ToInt32(telemetry.Tags.FirstOrDefault(t => t.Name == "duration")?.Value);
            Console.WriteLine($"MessageCreated {telemetry.Created} pulses {pulses} duration {duration} Type {telemetry.Type} DeviceID {telemetry.DeviceId}");
            if (pulses > 0)
            {
                if (telemetry.IsLine)
                {
                    await telemetryContext.Database.ExecuteSqlRawAsync("dbo.InsertPulseTelemetries @p0, @p1, @p2, @p3, @p4, @p5, @p6", telemetry.Type, telemetry.DeviceId, telemetry.Created.ToString("yyyy-MM-dd HH:mm:ss"), pulses, duration, telemetry.IsLine, tlemetryJson);
                }
                else
                {
                    await telemetryContext.Database.ExecuteSqlRawAsync("dbo.sp_InsertTelemetries @p0, @p1, @p2, @p3, @p4, @p5", telemetry.Type, telemetry.DeviceId, telemetry.Created.ToString("yyyy-MM-dd HH:mm:ss"), pulses, duration, tlemetryJson);
                }
            }
        }

        /// <summary>
        /// Saves the telemetry data to database.
        /// </summary>
        /// <param name="telemetryContext">The telemetry context.</param>
        /// <param name="telemetryData">The telemetry data.</param>
        /// <param name="telemetry">The equipment telemetry.</param>
        /// <param name="assemblytelemetry">The assembly telemetry.</param>
        public static void SaveTelemetryDataToDb(TelemetryContext telemetryContext, string telemetryData, EquipmentTelemetryModel telemetry, AssemblyLineModelVm assemblytelemetry)
        {
            StringBuilder message = new ();
            switch (telemetry.Type)
            {
                case TelemetryTypes.Plc:
                case TelemetryTypes.Valves:
                case TelemetryTypes.SensorTelemetry:
                case TelemetryTypes.TXPLEMDAI:
                case TelemetryTypes.TXKGEMDAI:
                    message.AppendFormat("Telemetry saving started.{0}", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    Telemetry equipmentTelemetry = new Telemetry
                    {
                        Type = telemetry.Type,
                        DeviceId = telemetry.DeviceId,
                        CycleTime = GetCycleTimeFromTag(telemetry.Tags),
                        IsLine = telemetry.IsLine,
                        CoolingTime = telemetry.Tags.FirstOrDefault(t => t.Name == "coolingtime")?.Value,
                        RPM = telemetry.Tags.FirstOrDefault(t => t.Name == "pulses")?.Value,
                        InjectionTime = telemetry.Tags.FirstOrDefault(t => t.Name == "injectiontime")?.Value,
                        OpenTime = telemetry.Tags.FirstOrDefault(t => t.Name == "opentime")?.Value,
                        CloseTime = telemetry.Tags.FirstOrDefault(t => t.Name == "closetime")?.Value,
                        RefillTime = telemetry.Tags.FirstOrDefault(t => t.Name == "refilltime")?.Value,
                        Temperature = telemetry.Tags.FirstOrDefault(t => t.Name == "Temperature")?.Value,
                        Viscosity = telemetry.Tags.FirstOrDefault(t => t.Name == "AmpsReading")?.Value,
                        Pressure = telemetry.Tags.FirstOrDefault(t => t.Name == "Pressure")?.Value,
                        Current = telemetry.Tags.FirstOrDefault(t => t.Name == "Current")?.Value,
                        Vibration = telemetry.Tags.FirstOrDefault(t => t.Name == "Vibration")?.Value,
                        Concentration = telemetry.Tags.FirstOrDefault(t => t.Name == "concentration")?.Value,
                        Production = 1,
                        PulsesPerMinute = 0,
                        IsProcess = false,
                        TelemetryJson = telemetryData,
                    };
                    equipmentTelemetry.Vibration = telemetry.Tags.FirstOrDefault(t => t.Name == "RPM")?.Value;
                    equipmentTelemetry.MessageCreated = telemetry.Created;
                    equipmentTelemetry.Created = DateTime.UtcNow;
                    telemetryContext.Telemetries.Add(equipmentTelemetry);
                    message.Clear();
                    message.AppendFormat("Telemetry message saved for Device Id: {0}.", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    break;
                case TelemetryTypes.Packing:
                    message.AppendFormat("Packing saving started.{0}", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    PackingTelemetry packingTelemetry = new PackingTelemetry
                    {
                        Type = telemetry.Type,
                        DeviceId = telemetry.DeviceId,
                        CycleTime = GetCycleTimeFromTag(telemetry.Tags),
                        CoolingTime = telemetry.Tags.FirstOrDefault(t => t.Name == "coolingtime")?.Value,
                        RPM = telemetry.Tags.FirstOrDefault(t => t.Name == "pulses")?.Value,
                        InjectionTime = telemetry.Tags.FirstOrDefault(t => t.Name == "injectiontime")?.Value,
                        OpenTime = telemetry.Tags.FirstOrDefault(t => t.Name == "opentime")?.Value,
                        CloseTime = telemetry.Tags.FirstOrDefault(t => t.Name == "closetime")?.Value,
                        RefillTime = telemetry.Tags.FirstOrDefault(t => t.Name == "refilltime")?.Value,
                        DI1 = telemetry.Tags.FirstOrDefault(t => t.Name == "DI1")?.Value,
                        DI2 = telemetry.Tags.FirstOrDefault(t => t.Name == "DI2")?.Value,
                        DI3 = telemetry.Tags.FirstOrDefault(t => t.Name == "DI3")?.Value,
                        DI4 = telemetry.Tags.FirstOrDefault(t => t.Name == "DI4")?.Value,
                        DI5 = telemetry.Tags.FirstOrDefault(t => t.Name == "DI5")?.Value,
                        DI6 = telemetry.Tags.FirstOrDefault(t => t.Name == "DI6")?.Value,
                        Temperature = telemetry.Tags.FirstOrDefault(t => t.Name == "Temperature")?.Value,
                        Viscosity = telemetry.Tags.FirstOrDefault(t => t.Name == "AmpsReading")?.Value,
                        Pressure = telemetry.Tags.FirstOrDefault(t => t.Name == "Pressure")?.Value,
                        Current = telemetry.Tags.FirstOrDefault(t => t.Name == "Current")?.Value,
                        Vibration = telemetry.Tags.FirstOrDefault(t => t.Name == "Vibration")?.Value,
                        Concentration = telemetry.Tags.FirstOrDefault(t => t.Name == "concentration")?.Value,
                        IsProcess = false,
                        TelemetryJson = telemetryData,
                    };
                    packingTelemetry.Vibration = telemetry.Tags.FirstOrDefault(t => t.Name == "RPM")?.Value;
                    packingTelemetry.MessageCreated = telemetry.Created;
                    packingTelemetry.Created = DateTime.UtcNow;
                    telemetryContext.PackingTelemetries.Add(packingTelemetry);
                    message.Clear();
                    message.AppendFormat("Packing Telemetry message saved for Device Id: {0}.", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    break;

                case TelemetryTypes.RejectionPulse:
                    message.AppendFormat("RejectionPulse saving started.{0}", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    RejectionTelemetry rejectionTelemetry = new RejectionTelemetry
                    {
                        Type = telemetry.Type,
                        DeviceId = telemetry.DeviceId,
                        CycleTime = GetCycleTimeFromTag(telemetry.Tags),
                        RejectionCode = telemetry.Tags.Find(t => t.Name == "rejectioncode")?.Value,
                        RejectionCount = Convert.ToDouble(telemetry.Tags.Find(t => t.Name == "rejectioncount")?.Value),
                        IsProcess = false,
                        TelemetryJson = telemetryData,
                        IsLine = telemetry.IsLine,
                    };
                    rejectionTelemetry.MessageCreated = telemetry.Created;
                    rejectionTelemetry.Created = DateTime.UtcNow;
                    telemetryContext.RejectionTelemetries.Add(rejectionTelemetry);
                    message.Clear();
                    message.AppendFormat("rejection Telemetry message saved for Device Id: {0}.", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    break;

                case TelemetryTypes.TXKGEMISP:
                    message.AppendFormat("Telemetry saving started.{0}", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    Telemetry jobTelemetry = new ()
                    {
                        Type = telemetry.Type,
                        DeviceId = telemetry.DeviceId,
                        CycleTime = GetCycleTimeFromTag(telemetry.Tags),
                        Throughput = telemetry.Tags.FirstOrDefault(t => t.Name == "Throughput")?.Value,
                        WeightPerMeter = telemetry.Tags.FirstOrDefault(t => t.Name == "WeightPerMeter")?.Value,
                        InputFlow = ConvertValueIntoDouble(telemetry.Tags.FirstOrDefault(t => t.Name == "Input flow")),
                        InputMaterial = ConvertValueIntoDouble(telemetry.Tags.FirstOrDefault(t => t.Name == "Input material")),
                        Quantity = ConvertValueIntoDouble(telemetry.Tags.FirstOrDefault(t => t.Name == "Quantity")),
                        Product = telemetry.Tags.FirstOrDefault(t => t.Name == "Product")?.Value,
                        FlowRate = ConvertValueIntoDouble(telemetry.Tags.FirstOrDefault(t => t.Name == "Flow rate")),
                        Width = telemetry.Tags.FirstOrDefault(t => t.Name == "Width")?.Value,
                        Thickness = telemetry.Tags.FirstOrDefault(t => t.Name == "Thickness")?.Value,
                        WorkOrderNo = telemetry.Tags.FirstOrDefault(t => t.Name == "WorkOrderNo")?.Value,
                        MeterPerMintue = telemetry.Tags.FirstOrDefault(t => t.Name == "meterpermintue")?.Value,
                        MessageCreated = telemetry.Created,
                        Created = DateTime.UtcNow,
                        Production = 1,
                        PulsesPerMinute = 0,
                        IsProcess = false,
                        TelemetryJson = telemetryData,
                    };
                    telemetryContext.Telemetries.Add(jobTelemetry);
                    message.AppendFormat("Telemetry message saved for Device Id: {0}.", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    break;
                case TelemetryTypes.TXKGEMESP:
                    message.AppendFormat("Telemetry saving started.{0}", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    Telemetry mainettiJobTelemetry = new Telemetry
                    {
                        Type = telemetry.Type,
                        DeviceId = telemetry.DeviceId,
                        CycleTime = GetCycleTimeFromTag(telemetry.Tags),
                        InputFlow = ConvertValueIntoDouble(telemetry.Tags.FirstOrDefault(t => t.Name == "Input flow")),
                        InputMaterial = ConvertValueIntoDouble(telemetry.Tags.FirstOrDefault(t => t.Name == "Input material")),
                        FlowRate = ConvertValueIntoDouble(telemetry.Tags.FirstOrDefault(t => t.Name == "Flow rate")),
                        MessageCreated = telemetry.Created,
                        Created = DateTime.UtcNow,
                        Production = ConvertValueIntoDouble(telemetry.Tags.FirstOrDefault(t => t.Name == "Production")),
                        PulsesPerMinute = 0,
                        IsProcess = false,
                        TelemetryJson = telemetryData,
                    };
                    telemetryContext.Telemetries.Add(mainettiJobTelemetry);
                    message.AppendFormat("Telemetry message saved for Device Id: {0}.", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    break;
                case TelemetryTypes.TXLNEMDAI:
                    message.AppendFormat("Telemetry saving started.{0}", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());

                    Telemetry meterTelemetry = new Telemetry
                    {
                        Type = telemetry.Type,
                        DeviceId = telemetry.DeviceId,
                        CycleTime = GetCycleTimeFromTag(telemetry.Tags),
                        MeterPerMintue = telemetry.Tags.FirstOrDefault(t => t.Name == "meter_per_minute")?.Value,
                        MessageCreated = telemetry.Created,
                        Production = 1,
                        PulsesPerMinute = 0,
                        Created = DateTime.UtcNow,
                        IsProcess = false,
                        TelemetryJson = telemetryData,
                    };
                    telemetryContext.Telemetries.Add(meterTelemetry);
                    message.AppendFormat("Telemetry message saved for Device Id: {0}.", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    break;
                case TelemetryTypes.Health:
                    var healthData = JsonConvert.DeserializeObject<DeviceLogTelemetry>(telemetryData);
                    message.AppendFormat("Health telemetry saving started.{0}", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());

                    HealthTelemetry healthTelemetry = new HealthTelemetry();
                    healthTelemetry.DeviceId = healthData.DeviceId;
                    healthTelemetry.MessageCreated = healthData.Created;
                    healthTelemetry.Status = healthData.Status;
                    healthTelemetry.StatusReason = healthData.StatusReason;
                    healthTelemetry.WiFiSignalStrength = healthData.DeviceInformation.WiFiSignalStrength;
                    healthTelemetry.Created = DateTime.UtcNow;
                    healthTelemetry.NodeId = healthData.NodeId;
                    healthTelemetry.SensorType = healthData.SensorType;
                    healthTelemetry.PlcStatus = healthData.PlcStatus;
                    telemetryContext.HealthTelemetries.Add(healthTelemetry);
                    message.AppendFormat("Telemetry message saved for Device Id: {0}.", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    break;
                case TelemetryTypes.Energy:
                case TelemetryTypes.EnergyOnDevice:
                case TelemetryTypes.EnergyOnMeter:
                    var energyData = JsonConvert.DeserializeObject<EnergyTelemetryModel>(telemetryData);
                    message.AppendFormat("Energy telemetry saving started.{0}", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    EnergyTelemetry energyTelemetry = new EnergyTelemetry();
                    energyTelemetry.Type = telemetry.Type;
                    energyTelemetry.MessageCreated = energyData.Created;
                    energyTelemetry.TotalActiveEnergy = telemetry.Tags.FirstOrDefault(t => t.Name == "TotalActiveEnergy")?.Value;
                    energyTelemetry.TotalReactiveEnergy = telemetry.Tags.FirstOrDefault(t => t.Name == "TotalReactiveEnergy")?.Value;
                    energyTelemetry.Phase1Current = telemetry.Tags.FirstOrDefault(t => t.Name == "Phase1Current")?.Value;
                    energyTelemetry.Phase2Current = telemetry.Tags.FirstOrDefault(t => t.Name == "Phase2Current")?.Value;
                    energyTelemetry.Phase3Current = telemetry.Tags.FirstOrDefault(t => t.Name == "Phase3Current")?.Value;
                    energyTelemetry.EnergyDuration = energyData.EnergyDuration;
                    energyTelemetry.DeviceId = energyData.DeviceId;
                    energyTelemetry.Created = DateTime.UtcNow;
                    telemetryContext.EnergyTelemetries.Add(energyTelemetry);
                    message.AppendFormat("Energy telemetry message saved for Device Id: {0}. ", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    if (string.IsNullOrEmpty(energyTelemetry.TotalActiveEnergy))
                    {
                        message.Clear();

                        message.AppendFormat("Energy values are null for Device Id: {0}. ", telemetry.DeviceId);
                        Console.WriteLine(message.ToString());
                    }

                    break;

                case TelemetryTypes.RawTelemetry:
                    message.AppendFormat("Raw telemetry saving started.{0}", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    RawTelemetry rawTelemetry = new RawTelemetry();
                    rawTelemetry.DeviceId = telemetry.DeviceId;
                    rawTelemetry.MessageCreated = telemetry.Created;
                    rawTelemetry.DI1 = (telemetry.Tags.FirstOrDefault(t => t.Name == "DI1")?.Value).StringToBoolean();
                    rawTelemetry.DI2 = (telemetry.Tags.FirstOrDefault(t => t.Name == "DI2")?.Value).StringToBoolean();
                    rawTelemetry.DI3 = (telemetry.Tags.FirstOrDefault(t => t.Name == "DI3")?.Value).StringToBoolean();
                    rawTelemetry.DI4 = (telemetry.Tags.FirstOrDefault(t => t.Name == "DI4")?.Value).StringToBoolean();
                    rawTelemetry.DI5 = (telemetry.Tags.FirstOrDefault(t => t.Name == "DI5")?.Value).StringToBoolean();
                    rawTelemetry.DI6 = (telemetry.Tags.FirstOrDefault(t => t.Name == "DI6")?.Value).StringToBoolean();
                    rawTelemetry.A1 = Convert.ToInt64(telemetry.Tags.FirstOrDefault(t => t.Name == "A1")?.Value);
                    rawTelemetry.A2 = Convert.ToInt64(telemetry.Tags.FirstOrDefault(t => t.Name == "A2")?.Value);
                    rawTelemetry.Created = DateTime.UtcNow;
                    telemetryContext.RawTelemetries.Add(rawTelemetry);
                    message.AppendFormat("Raw telemetry message saved for Device Id: {0}.", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    break;

                case TelemetryTypes.SilosTelemetry:
                    message.AppendFormat("Silos telemetry saving started.{0}", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    var silosData = JsonConvert.DeserializeObject<SilosTelemetryModel>(telemetryData);
                    SilosTelemetry silosTelemetry = new SilosTelemetry();
                    silosTelemetry.DeviceId = silosData.DeviceId;
                    silosTelemetry.MessageCreated = silosData.Created;
                    silosTelemetry.CurrentValue = Convert.ToDouble(silosData.Tags.FirstOrDefault(t => t.Name == "CurrentValue")?.Value);
                    silosTelemetry.Name = silosData.Tags.FirstOrDefault(t => t.Name == "Name")?.Value;
                    silosTelemetry.Product = silosData.Tags.FirstOrDefault(t => t.Name == "Product")?.Value;
                    silosTelemetry.MaxWeight = Convert.ToInt32(silosData.Tags.FirstOrDefault(t => t.Name == "MaxWeight")?.Value);
                    silosTelemetry.MinWeight = Convert.ToInt32(silosData.Tags.FirstOrDefault(t => t.Name == "LowWeight")?.Value);
                    silosTelemetry.UrgentWeight = Convert.ToInt32(silosData.Tags.FirstOrDefault(t => t.Name == "UrgentWeight")?.Value);
                    silosTelemetry.ReorderWeight = Convert.ToInt32(silosData.Tags.FirstOrDefault(t => t.Name == "ReorderWeight")?.Value);
                    silosTelemetry.PercentFull = Convert.ToInt32(silosData.Tags.FirstOrDefault(t => t.Name == "PercentFull")?.Value);
                    silosTelemetry.UsageRate = silosData.Tags.FirstOrDefault(t => t.Name == "UsageRate")?.Value;
                    silosTelemetry.NormalUsageRate = Convert.ToInt32(silosData.Tags.FirstOrDefault(t => t.Name == "NormalUsageRate")?.Value);
                    silosTelemetry.Minutes = Convert.ToInt32(silosData.Tags.FirstOrDefault(t => t.Name == "Minutes")?.Value);
                    silosTelemetry.Created = DateTime.UtcNow;
                    silosTelemetry.IsProcess = false;
                    silosTelemetry.SilosJson = telemetryData;
                    telemetryContext.SilosTelemetries.Add(silosTelemetry);
                    message.AppendFormat("Silos telemetry message saved for Device Id: {0}.", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    break;

                case TelemetryTypes.VisionTelemetry:
                    var visioTtelemetry = JsonConvert.DeserializeObject<VisionTelemetryModel>(telemetryData);

                    foreach (var tag in visioTtelemetry.Tags)
                    {
                        if (tag != null && !string.IsNullOrEmpty(tag?.TrackerId))
                        {
                            Console.WriteLine("Vision telemetry saving started.");
                            var checkNo = double.TryParse(tag.BodyTemp, out _);
                            Console.WriteLine($"Vision Telemetry BodyTemperature = {tag.BodyTemp} and TrackerId = {tag.TrackerId} and ROIID = {visioTtelemetry.EquipmentId} and IsNumber = {checkNo} and CheckInTime = {tag.CheckInTime}");
                            VisionTelemetry visionTelemetry = new VisionTelemetry
                            {
                                Confidence = tag.Confidence,
                                ObjectId = tag.ObjectId,
                                Time = tag.Time,
                                XAxis = visioTtelemetry.X > 0 ? visioTtelemetry.X : tag.XAxis,
                                YAxis = visioTtelemetry.Y > 0 ? visioTtelemetry.Y : tag.YAxis,
                                MessageId = visioTtelemetry.Id,
                                MessageCreated = visioTtelemetry.Created,
                                ROIId = visioTtelemetry.EquipmentId,
                                TrackerId = tag.TrackerId,
                                Helmet = tag.Helmet,
                                Jacket = tag.Jacket,
                                JacketColor = tag.JacketColor,
                                Created = DateTime.UtcNow,
                                BodyTemp = checkNo ? Convert.ToDouble(tag.BodyTemp) : 0,
                                CheckInTime = tag.CheckInTime,
                            };

                            telemetryContext.VisionTelemetries.Add(visionTelemetry);
                            Console.WriteLine("Vision telemetry saving Finished.");
                        }
                    }

                    if (visioTtelemetry.ROIId?.ToLower() != "check-in-area".ToLower())
                    {
                        if (string.Equals(visioTtelemetry.ROIId, "pathway", StringComparison.InvariantCultureIgnoreCase) && visioTtelemetry.Tags.Count == 0)
                        {
                            VisionTelemetry visionTelemetry = new VisionTelemetry
                            {
                                XAxis = visioTtelemetry.X,
                                YAxis = visioTtelemetry.Y,
                                MessageId = visioTtelemetry.Id,
                                MessageCreated = visioTtelemetry.Created,
                                ROIId = visioTtelemetry.EquipmentId,
                                Created = DateTime.UtcNow,
                            };

                            telemetryContext.VisionTelemetries.Add(visionTelemetry);
                        }

                        RawPersonTelemetry rawPersonTelemetry = new RawPersonTelemetry
                        {
                            MessageId = visioTtelemetry.Id,
                            MessageCreated = visioTtelemetry.Created,
                            ROIId = visioTtelemetry.ROIId,
                            Created = DateTime.UtcNow,
                            TotalPersonCount = visioTtelemetry.TotalPersonCount,
                        };
                        telemetryContext.RawPersonTelemetries.Add(rawPersonTelemetry);

                        foreach (var tag in visioTtelemetry.SocialDistanceViolations)
                        {
                            Console.WriteLine("Social distance violation telemetry saving started.");
                            SocialDVTelemetries socialDVTelemetries = new SocialDVTelemetries
                            {
                                Created = DateTime.UtcNow,
                                Location = string.Join(",", tag.Location),
                                MessageCreated = visioTtelemetry.Created,
                                MessageId = visioTtelemetry.Id,
                                Time = tag.Time,
                                Type = visioTtelemetry.Type,
                                ROIId = visioTtelemetry.ROIId,
                                PersonCount = tag.PersonCount,
                                ObjectId = tag.ObjectId,
                            };

                            telemetryContext.SocialDVTelemetries.Add(socialDVTelemetries);
                            Console.WriteLine("Social distance violation telemetry saving Finished.");
                        }
                    }

                    break;

                case TelemetryTypes.Temperature:
                    Console.WriteLine($"Ambient temperature telemetry saving started. {telemetry.DeviceId}");
                    break;

                case TelemetryTypes.Assembly:
                    message.AppendFormat("Assembly telemetry saving started.{0}", telemetry.DeviceId);
                    AssemblyLineTelemetry assemblyLineTelemetry = new AssemblyLineTelemetry();
                    assemblyLineTelemetry.Created = assemblytelemetry.Created;
                    assemblyLineTelemetry.Type = assemblytelemetry.Type;
                    assemblyLineTelemetry.EquipmentId = assemblytelemetry.EquipmentId;
                    assemblyLineTelemetry.DeviceId = assemblytelemetry.DeviceId;
                    assemblyLineTelemetry.AssemblyCycleTime = assemblytelemetry.AssemblyDetails.AssemblyCycleTime;
                    assemblyLineTelemetry.AssemblyImageUrl = assemblytelemetry.AssemblyDetails.AssemblyImageUrl;
                    assemblyLineTelemetry.Station1CycleTime = assemblytelemetry.StationDetails.Where(c => c.Station1CycleTime != null).Select(a => a.Station1CycleTime).FirstOrDefault() ?? null;
                    assemblyLineTelemetry.Station1ImageUrl = assemblytelemetry.StationDetails.Where(c => c.Station1ImageUrl != null).Select(a => a.Station1ImageUrl).FirstOrDefault() ?? null;
                    assemblyLineTelemetry.Station2CycleTime = assemblytelemetry.StationDetails.Where(c => c.Station2CycleTime != null).Select(a => a.Station2CycleTime).FirstOrDefault() ?? null;
                    assemblyLineTelemetry.Station2ImageUrl = assemblytelemetry.StationDetails.Where(c => c.Station2ImageUrl != null).Select(a => a.Station2ImageUrl).FirstOrDefault() ?? null;
                    assemblyLineTelemetry.Station3CycleTime = assemblytelemetry.StationDetails.Where(c => c.Station3CycleTime != null).Select(a => a.Station3CycleTime).FirstOrDefault() ?? null;
                    assemblyLineTelemetry.Station3ImageUrl = assemblytelemetry.StationDetails.Where(c => c.Station3ImageUrl != null).Select(a => a.Station3ImageUrl).FirstOrDefault() ?? null;
                    assemblyLineTelemetry.Station4CycleTime = assemblytelemetry.StationDetails.Where(c => c.Station4CycleTime != null).Select(a => a.Station4CycleTime).FirstOrDefault() ?? null;
                    assemblyLineTelemetry.Station4ImageUrl = assemblytelemetry.StationDetails.Where(c => c.Station4ImageUrl != null).Select(a => a.Station4ImageUrl).FirstOrDefault() ?? null;
                    assemblyLineTelemetry.Station5CycleTime = assemblytelemetry.StationDetails.Where(c => c.Station5CycleTime != null).Select(a => a.Station5CycleTime).FirstOrDefault() ?? null;
                    assemblyLineTelemetry.Station5ImageUrl = assemblytelemetry.StationDetails.Where(c => c.Station5ImageUrl != null).Select(a => a.Station5ImageUrl).FirstOrDefault() ?? null;

                    telemetryContext.AssemblyLineTelemetries.Add(assemblyLineTelemetry);
                    message.AppendFormat(" Assembly telemetry message saved for Device Id: {0}.", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    break;

                case TelemetryTypes.VisionAssembly:
                    message.AppendFormat("Vision Assembly saving started.{0}", telemetry.DeviceId);
                    Telemetry visionAssembly = new Telemetry
                    {
                        Type = telemetry.Type,
                        DeviceId = telemetry.DeviceId,
                        CycleTime = GetCycleTimeFromTag(telemetry.Tags),
                        CoolingTime = telemetry.Tags.FirstOrDefault(t => t.Name == "coolingtime")?.Value,
                        RPM = telemetry.Tags.FirstOrDefault(t => t.Name == "pulses")?.Value,
                        InjectionTime = telemetry.Tags.FirstOrDefault(t => t.Name == "injectiontime")?.Value,
                        OpenTime = telemetry.Tags.FirstOrDefault(t => t.Name == "opentime")?.Value,
                        CloseTime = telemetry.Tags.FirstOrDefault(t => t.Name == "closetime")?.Value,
                        RefillTime = telemetry.Tags.FirstOrDefault(t => t.Name == "refilltime")?.Value,
                        Temperature = telemetry.Tags.FirstOrDefault(t => t.Name == "Temperature")?.Value,
                        Viscosity = telemetry.Tags.FirstOrDefault(t => t.Name == "AmpsReading")?.Value,
                        Pressure = telemetry.Tags.FirstOrDefault(t => t.Name == "Pressure")?.Value,
                        Current = telemetry.Tags.FirstOrDefault(t => t.Name == "Current")?.Value,
                        Vibration = telemetry.Tags.FirstOrDefault(t => t.Name == "Vibration")?.Value,
                        Concentration = telemetry.Tags.FirstOrDefault(t => t.Name == "concentration")?.Value,
                        Production = 1,
                        PulsesPerMinute = 0,
                        IsProcess = false,
                        TelemetryJson = telemetryData,
                    };
                    visionAssembly.Vibration = telemetry.Tags.FirstOrDefault(t => t.Name == "RPM")?.Value;
                    visionAssembly.MessageCreated = telemetry.Created;
                    visionAssembly.Created = DateTime.UtcNow;
                    telemetryContext.Telemetries.Add(visionAssembly);
                    telemetryContext.SaveChanges();
                    List<TelemetrySteps> telemtrysteplist = new List<TelemetrySteps>();
                    foreach (var stp in telemetry.Steps)
                    {
                        TelemetrySteps telemtrystep = new TelemetrySteps
                        {
                            Name = stp.Name,
                            StartTime = stp.StartTime,
                            EndTime = stp.EndTime,
                            IsCompleted = stp.IsCompleted,
                            TelemetryId = visionAssembly.Id,
                            CameraId = telemetry.CameraId,
                        };
                        foreach (var data in stp.MetaData)
                        {
                            TelemetryStepMetaData metadata = new TelemetryStepMetaData
                            {
                                ActualCount = data.ActualCount,
                                ExpectedCount = data.ExpectedCount,
                            };
                            telemtrystep.TelemetryStepMetaDatas.Add(metadata);
                        }

                        telemtrysteplist.Add(telemtrystep);
                    }

                    if (telemtrysteplist.Count > 0)
                    {
                        telemetryContext.TelemetrySteps.AddRange(telemtrysteplist);
                    }

                    message.Clear();
                    message.AppendFormat("Vision Assembly message saved for Device Id: {0}.", telemetry.DeviceId);
                    Console.WriteLine(message.ToString());
                    break;
            }

            Log.Information(telemetryData);
        }

        /// <summary>
        /// Gets the cycle time from tag.
        /// </summary>
        /// <param name="tagModel">The tag model.</param>
        /// <returns>
        /// cycle time.
        /// </returns>
        public static double GetCycleTimeFromTag(List<TagModel> tagModel)
        {
            double.TryParse(tagModel.Find(t => t.Name == "cycletime")?.Value, out double cycleTime);
            return cycleTime > 0 ? cycleTime : 0;
        }

        /// <summary>
        /// Gets the cycle time from tag.
        /// </summary>
        /// <param name="tagModel">The tag model.</param>
        /// <returns>
        /// cycle time.
        /// </returns>
        public static double ConvertValueIntoDouble(TagModel tagModel)
        {
            double.TryParse(tagModel?.Value, out double doubleValue);
            return doubleValue > 0 ? doubleValue : 0;
        }
    }
}
