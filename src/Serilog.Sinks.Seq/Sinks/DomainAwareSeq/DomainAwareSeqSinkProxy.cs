using System;
using Newtonsoft.Json;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.DomainAwareSeq.StephenCleary;
using Serilog.Sinks.Seq;

namespace Serilog.Sinks.DomainAwareSeq
{
    /// <summary>
    /// This proxy shall be used instead of Seq sink when there is a chance that multiple sinks may be created in different AppDomains.
    /// Proxy is created in each AppDomain separately and forwards Emit calls to domain aware singleton DomainAwareSeqSink.
    /// </summary>
    public class DomainAwareSeqSinkProxy : ILogEventSink
    {
        // In default AppDomain it is direct reference to DomainAwareSeqSink object.
        // In non-default AppDomain it is transparent proxy to marshall the call to the singleton in default AppDomain.
        private DomainAwareSeqSink _sink;

        // Just caching for speed
        private static readonly bool IsDefaultAppDomain = AppDomain.CurrentDomain.IsDefaultAppDomain();

        /// <summary>
        /// DomainAwareSeqSinkProxy constructor with the same parameters as regular Seq sink.
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="apiKey"></param>
        /// <param name="batchPostingLimit"></param>
        /// <param name="period"></param>
        public static ILogEventSink Instance(string serverUrl, string apiKey, int batchPostingLimit = SeqSink.DefaultBatchPostingLimit, TimeSpan? period = null)
        {
            var instance = new DomainAwareSeqSinkProxy
            {
                _sink = DomainAwareSingleton<DomainAwareSeqSink>.Instance
            };
            // AppDomain aware singleton initialization. All calls except the very first one in the process are ignored.

            instance._sink.Initialize(serverUrl, apiKey, batchPostingLimit, period ?? SeqSink.DefaultPeriod);

            return IsDefaultAppDomain ?
                instance._sink.GetUnderlyingSink() : instance;
        }

        /// <summary>
        /// This is passthrough directly to SeqSink for the case when logEvent was generated in default AppDomain,
        /// does not require marshaling and bypass pre-serialization in DomainAwareSeqSinkProxy
        /// </summary>
        /// <param name="logEvent">The log event.</param>
        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException("logEvent");

            // LogEvent generated in default AppDomain can take a shortcut and go stright to standard RollingSink.Emit method.
            if (IsDefaultAppDomain)
            {
                _sink.Emit(logEvent);
                return;
            }

            // When LogEvent is generated in non-default AppDomain it is pre-serialized to string and then sent to custom Emit method 
            // (which accepts strings) across AppDomain boundary. 
            _sink.Emit(JsonConvert.SerializeObject(logEvent));
        }
    }
}