//-----------------------------------------------------------------------
// <copyright file="EquipmentTelemetryModel.cs" company="ThingTrax UK Ltd">
// Copyright (c) ThingTrax Ltd. All rights reserved.
// </copyright>
// <summary>Telemetry model class.</summary>
//-----------------------------------------------------------------------

namespace TT.Core.Models.Telemetry
{
    using System;
    using System.Collections.Generic;
    using TT.Core.Models.Enums;

    /// <summary>
    /// Telemetry model class.
    /// </summary>
    /// <seealso cref="ModelBase" />
    public class EquipmentTelemetryModel : TelemetryBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EquipmentTelemetryModel"/> class.
        /// </summary>
        public EquipmentTelemetryModel()
        {
            this.Tags = new List<TagModel>();
        }

        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        /// <value>
        /// The device identifier.
        /// </value>
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is line.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is line; otherwise, <c>false</c>.
        /// </value>
        public bool IsLine { get; set; }

        /////// <summary>
        /////// Gets or sets the equipment identifier.
        /////// </summary>
        /////// <value>
        /////// The equipment identifier.
        /////// </value>
        ////public string EquipmentId { get; set; }

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        /// <value>
        /// The name of the service.
        /// </value>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public Status Status { get; set; }

        /// <summary>
        /// Gets or sets the Remarks.
        /// </summary>
        /// <value>
        /// The Remarks.
        /// </value>
        public string Remarks { get; set; }

        /// <summary>
        /// Gets or sets the status reason.
        /// </summary>
        /// <value>
        /// The status reason.
        /// </value>
        public StatusReason StatusReason { get; set; }

        /// <summary>
        /// Gets or sets the CameraId.
        /// </summary>
        /// <value>
        /// The CameraId.
        /// </value>
        public string CameraId { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>
        /// The tags.
        /// </value>
        public List<TagModel> Tags { get; set; }
    }

    public class TagModel
    {
        /// <summary>
        /// The value.
        /// </summary>
        private string value;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        /// <value>
        /// The label.
        /// </value>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Value
        {
            get
            {
                return this.value;
            }

            set
            {
                double dResult;
                if (double.TryParse(value, out dResult))
                {
                    this.value = Math.Round(dResult, 2).ToString();
                }
                else
                {
                    this.value = value;
                }
            }
        }
    }
}