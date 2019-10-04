using System;
using Newtonsoft.Json;

namespace Optimization
{
    /// <summary>
    /// Contains the walk forward optimization algorithm configuration
    /// </summary>
    [Serializable]
    public class WalkForwardConfiguration
    {
        /// <summary>
        /// Whether walking forward optimization mode is enabled or not
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// The Start date can move forward by step too, or can be anchored 
        /// </summary>
        [JsonProperty("anchored")]
        public bool? Anchored { get; set; }

        /// <summary>
        /// The length of segement of historical data to optimize the parameters values on
        /// </summary>
        /// <remarks>In days</remarks>
        [JsonProperty("in-sample-period")]
        public int? InSamplePeriod { get; set; }

        /// <summary>
        /// The length of immediately followed out-of-sample segement of historical data
        /// used for testing forward the best parameters obtained on in-sample-data.
        /// Also, the step to move forward a test window on every iteration. 
        /// </summary>
        /// /// <remarks>In days</remarks>
        [JsonProperty("step")]
        public int? Step { get; set; }
    }
}
