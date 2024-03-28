using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Neo4jLogger = Neo4j.Driver.ILogger;

namespace Neo4j.KernelMemory.MemoryStorage;

internal sealed class Neo4jDriverFactory
{
    public static IDriver BuildDriver(Neo4jConfig config, ILogger logger)
    {
        return GraphDatabase.Driver(config.Uri,
            AuthTokens.Basic(config.Username, config.Password),
            x => x.WithLogger(new Logger(logger))
        );
    }

    private sealed class Logger : Neo4jLogger
    {
        private readonly ILogger _logger;

        public Logger(ILogger logger)
        {
            _logger = logger;
        }

        public void Error(Exception? cause, string message, params object[] args)
        {
            if (cause != null)
                _logger.Log(LogLevel.Error, cause, message, args);
            else
                _logger.Log(LogLevel.Error, message, args);
        }

        public void Warn(Exception? cause, string message, params object[] args)
        {
            if (cause != null)
                _logger.Log(LogLevel.Warning, cause, message, args);
            else
                _logger.Log(LogLevel.Warning, message, args);
        }

        public void Info(string message, params object[] args)
        {
            _logger.Log(LogLevel.Information, message, args);
        }

        public void Debug(string message, params object[] args)
        {
            _logger.Log(LogLevel.Debug, message, args);
        }

        public void Trace(string message, params object[] args)
        {
            // NoOp
        }

        public bool IsTraceEnabled()
        {
            return false;
        }

        public bool IsDebugEnabled()
        {
            return true;
        }
    }
}