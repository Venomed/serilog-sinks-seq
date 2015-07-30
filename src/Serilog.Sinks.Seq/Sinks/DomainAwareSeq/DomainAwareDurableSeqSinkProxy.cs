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
    public class DomainAwareDurableSeqSinkProxy : ILogEventSink
    {
        // In default AppDomain it is direct reference to DomainAwareDurableSeqSinkProxy object.
        // In non-default AppDomain it is transparent proxy to marshall the call to the singleton in default AppDomain.
        private DomainAwareDurableSeqSink _sink;

        private DomainAwareDurableSeqSinkProxy() { }

        // Just caching for speed
        private static readonly bool IsDefaultAppDomain = AppDomain.CurrentDomain.IsDefaultAppDomain();

        /// <summary>
        /// DomainAwareSeqSinkProxy constructor with the same parameters as regular Seq sink.
        /// </summary>
        /// <param name="serverUrl">The base URL of the Seq server that log events will be written to.</param>
        /// <param name="apiKey">A Seq <i>API key</i> that authenticates the client to the Seq server.</param>
        /// <param name="bufferBaseFilename">Path for a set of files that will be used to buffer events until they
        /// can be successfully transmitted across the network. Individual files will be created using the
        /// pattern <paramref name="bufferBaseFilename"/>-{Date}.json.</param>
        /// <param name="batchPostingLimit"></param>
        /// <param name="period"></param>
        /// <param name="bufferFileSizeLimitBytes">The maximum size, in bytes, to which the buffer
        /// log file for a specific date will be allowed to grow. By default no limit will be applied.</param>
        public static ILogEventSink Instance(string serverUrl, string apiKey, string bufferBaseFilename, int batchPostingLimit = SeqSink.DefaultBatchPostingLimit, TimeSpan? period = null, long? bufferFileSizeLimitBytes = 1073741824)
        {
            var instance = new DomainAwareDurableSeqSinkProxy
            {
                _sink = DomainAwareSingleton<DomainAwareDurableSeqSink>.Instance
            };

            // AppDomain aware singleton initialization. All calls except the very first one in the process are ignored.
            instance._sink.Initialize(serverUrl, apiKey, batchPostingLimit, period ?? SeqSink.DefaultPeriod, bufferBaseFilename, bufferFileSizeLimitBytes);

            return IsDefaultAppDomain ?
                instance._sink.GetUnderlyingSink() : instance;
        }

        /// <summary>
        /// This is passthrough directly to DurableSeqSink for the case when logEvent was generated in default AppDomain,
        /// does not require marshaling and bypass pre-serialization in DomainAwareDurableSeqSinkProxy
        /// </summary>
        /// <param name="logEvent">The log event.</param>
        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException("logEvent");

            // LogEvent generated in default AppDomain can take a shortcut and go stright to standard DurableSeqSink.Emit method.
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