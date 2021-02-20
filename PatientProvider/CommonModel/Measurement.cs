using System;

namespace CommonModel
{
    public class Measurement
    {
        public int MeasurementId { get; set; }

        public string MeasurementDate { get; set; }

        public float MeasurementValue { get; set; }

        public bool IsProblem { get; set; }
    }
}
