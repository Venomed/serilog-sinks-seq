﻿using System;
using System.Security.Permissions;
using Newtonsoft.Json;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Seq;

namespace Serilog.Sinks.DomainAwareSeq
{
    /// <summary>
    /// An appdomain aware seq server sink. This class cannot be inherited.
    /// </summary>
    public sealed class DomainAwareSeqSink : MarshalByRefObject
    {
        private SeqSink _sink;
        private readonly object _initializeSyncRoot = new object();
        
        /// <summary>
        /// AppDomain aware singleton initialization. All calls except the very first one in the process are ignored.
        /// </summary>
        /// <param name="serverUrl">Url of Seq server</param>
        /// <param name="apiKey">API Key if you have any</param>
        /// <param name="batchPostingLimit">Number of events posted per batch</param>
        /// <param name="period">Time to wait between checking for event batches</param>
        public void Initialize(string serverUrl, string apiKey, int batchPostingLimit, TimeSpan period)
        {
            lock (_initializeSyncRoot)
            {
                if (_sink != null) return;

                _sink = new SeqSink(serverUrl, apiKey, batchPostingLimit, period);
            }
        }

        /// <summary>
        /// Shortcut for the case when proxy and singleton are both in the same (default) domain.
        /// </summary>
        /// <returns>
        /// The underlying sink.
        /// </returns>
        public ILogEventSink GetUnderlyingSink()
        {
            return _sink;
        }

        /// <summary>
        /// Obtains a lifetime service object to control the lifetime policy for this instance.
        /// This particular implementation returns null resulting in infinite lifetime.
        /// This class is only intended to be used in Singleton scenarios where it has the same lifetime as the application.
        /// </summary>
        /// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return null;
        }

        /// <summary>
        /// This is passthrough directly to SeqSink for the case when logEvent was generated in default AppDomain,
        /// does not require marshaling and bypass pre-serialization in DomainAwareSeqSinkProxy
        /// </summary>
        /// <param name="logEvent">The log event.</param>
        public void Emit(LogEvent logEvent)
        {
            _sink.Emit(logEvent);
        }

        /// <summary>
        /// This is custom variation of Emit method which takes a string, rather than LogEvent.
        /// String supposed to be generated by LogEvent serialization in DomainAwareRollingFileSinkProxy and come here across domain boundary.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when logEventString argument is null.</exception>
        /// <param name="logEventString">The log event json serialized string.</param>
        public void Emit(string logEventString)
        {
            if (logEventString == null) throw new ArgumentNullException("logEventString");

            _sink.Emit(JsonConvert.DeserializeObject<LogEvent>(logEventString));
        }
    }
}
