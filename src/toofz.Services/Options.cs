﻿using System;

namespace toofz.Services
{
    /// <summary>
    /// Base set of options supported by services.
    /// </summary>
    public class Options
    {
        /// <summary>
        /// Gets or a sets a value indicating whether to show usage information.
        /// </summary>
        public bool ShowHelp { get; internal set; }
        /// <summary>
        /// The minimum amount of time that should pass between the start of each cycle.
        /// </summary>
        public TimeSpan? UpdateInterval { get; internal set; }
        /// <summary>
        /// The amount of time to wait after a cycle to perform garbage collection.
        /// </summary>
        public TimeSpan? DelayBeforeGC { get; internal set; }
        /// <summary>
        /// An Application Insights instrumentation key.
        /// </summary>
        public string InstrumentationKey { get; internal set; }
        /// <summary>
        /// The number of rounds to execute a key derivation function.
        /// </summary>
        public int? KeyDerivationIterations { get; internal set; }
        /// <summary>
        /// The connection string used to connect to the leaderboards database.
        /// </summary>
        public string LeaderboardsConnectionString { get; internal set; } = "";
    }
}
