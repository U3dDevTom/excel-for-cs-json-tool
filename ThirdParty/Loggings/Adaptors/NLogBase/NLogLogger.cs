using System;

namespace ThirdParty.Loggings.Adaptors.NLogBase
{
    public sealed class NLogLogger<TConfig>
            : ILogger
        where TConfig: INLogConfig
    {
        private readonly NLog.Logger nlogger_;


        internal NLogLogger(
                TConfig config,
                string loggerName
            )
        {
            NLog.LogManager.Configuration = config.Configuration;
            this.nlogger_ = NLog.LogManager.GetLogger(loggerName);
        }


        #region (Exception, string, objects[])

        public void Debug(Exception exception, string format, params object[] args)
        {
            if (!this.nlogger_.IsDebugEnabled) {
                return;
            }
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Debug, this.nlogger_.Name,  exception, null, format, args
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Error(Exception exception, string format, params object[] args)
        {
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Error, this.nlogger_.Name, exception, null, format, args
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Fatal(Exception exception, string format, params object[] args)
        {
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Fatal, this.nlogger_.Name, exception, null, format, args
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Info(Exception exception, string format, params object[] args)
        {
            if (!this.nlogger_.IsInfoEnabled) {
                return;
            }
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Info, this.nlogger_.Name, exception, null, format, args
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Trace(Exception exception, string format, params object[] args)
        {
            if (!this.nlogger_.IsTraceEnabled) {
                return;
            }
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Trace, this.nlogger_.Name, exception, null, format, args
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Warn(Exception exception, string format, params object[] args)
        {
            if (!this.nlogger_.IsWarnEnabled) {
                return;
            }
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Warn, this.nlogger_.Name, exception, null, format, args
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }

        #endregion


        #region (string, object[])

        public void Debug(string format, params object[] args)
        {
            #if DEBUG
            if (!this.nlogger_.IsDebugEnabled)
            {
                return;
            }
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Debug, this.nlogger_.Name, null, format, args
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
            #endif  
        }


        public void Error(string format, params object[] args)
        {
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Error, this.nlogger_.Name, null, format, args
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Fatal(string format, params object[] args)
        {
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Fatal, this.nlogger_.Name, null, format, args
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Info(string format, params object[] args)
        {
            if (!this.nlogger_.IsInfoEnabled) {
                return;
            }
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Info, this.nlogger_.Name, null, format, args
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Trace(string format, params object[] args)
        {
#if DEBUG
            if (!this.nlogger_.IsTraceEnabled) {
                return;
            }
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Trace, this.nlogger_.Name, null, format, args
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
#endif
        }


        public void Warn(string format, params object[] args)
        {
            if (!this.nlogger_.IsWarnEnabled) {
                return;
            }
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Warn, this.nlogger_.Name, null, format, args
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }

#endregion


        public void Debug(Exception exception)
        {
            if (!this.nlogger_.IsDebugEnabled) {
                return;
            }
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Debug, this.nlogger_.Name, exception, null,
                exception.Format()
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Error(Exception exception)
        {
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Error, this.nlogger_.Name, exception, null,
                exception.Format()
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Fatal(Exception exception)
        {
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Fatal, this.nlogger_.Name, exception, null,
                exception.Format()
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Info(Exception exception)
        {
            if (!this.nlogger_.IsInfoEnabled) {
                return;
            }
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Info, this.nlogger_.Name, exception, null,
                exception.Format()
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Trace(Exception exception)
        {
            if (!this.nlogger_.IsTraceEnabled) {
                return;
            }
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Trace, this.nlogger_.Name, exception, null,
                exception.Format()
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }


        public void Warn(Exception exception)
        {
            if (!this.nlogger_.IsWarnEnabled) {
                return;
            }
            var logEvent = NLog.LogEventInfo.Create(
                NLog.LogLevel.Warn, this.nlogger_.Name, exception, null,
                exception.Format()
            );
            this.nlogger_.Log(typeof(NLogLogger<TConfig>), logEvent);
        }
    }
}
