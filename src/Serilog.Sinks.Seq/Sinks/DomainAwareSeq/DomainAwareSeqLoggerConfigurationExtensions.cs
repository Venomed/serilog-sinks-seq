using System;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.DomainAwareSeq.StephenCleary;
using Serilog.Sinks.Seq;

namespace Serilog.Sinks.DomainAwareSeq
{
    /// <summary>
    /// Extension methods surrogate class
    /// </summary>
    public static class DomainAwareSeqLoggerConfigurationExtensions
    {
        /// <summary>
        /// Adds a sink that writes log events to a http://getseq.net Seq event server.
        /// </summary>
        /// <param name="loggerSinkConfiguration">The logger configuration.</param>
        /// <param name="serverUrl">The base URL of the Seq server that log events will be written to.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required 
        /// in order to write an event to the sink.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="bufferBaseFilename">Path for a set of files that will be used to buffer events until they
        /// can be successfully transmitted across the network. Individual files will be created using the
        /// pattern <paramref name="bufferBaseFilename"/>-{Date}.json.</param>
        /// <param name="apiKey">A Seq <i>API key</i> that authenticates the client to the Seq server.</param>
        /// <param name="bufferFileSizeLimitBytes">The maximum size, in bytes, to which the buffer
        /// log file for a specific date will be allowed to grow. By default no limit will be applied.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration DomainAwareSeq(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string serverUrl,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = SeqSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            string apiKey = null,
            string bufferBaseFilename = null,
            long? bufferFileSizeLimitBytes = null)
        {
            if (loggerSinkConfiguration == null) throw new ArgumentNullException("loggerSinkConfiguration");
            if (serverUrl == null) throw new ArgumentNullException("serverUrl");
            if (bufferFileSizeLimitBytes.HasValue && bufferFileSizeLimitBytes < 0) throw new ArgumentException("Negative value provided; file size limit must be non-negative");

            var defaultedPeriod = period ?? SeqSink.DefaultPeriod;

            var sink = bufferBaseFilename == null ? DomainAwareSeqSinkProxy.Instance(serverUrl, apiKey, batchPostingLimit, period) :
                new DurableSeqSink(serverUrl, bufferBaseFilename, apiKey, batchPostingLimit, defaultedPeriod, bufferFileSizeLimitBytes);

            return loggerSinkConfiguration.Sink(sink, restrictedToMinimumLevel);
        }
    }
}
