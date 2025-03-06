using System;
using System.Collections.Generic;
using NLog.Targets.Wrappers;

namespace ThirdParty.Loggings.Adaptors.NLogBase
{
    public sealed class DefaultFactory : ILoggerFactory
    {
        private sealed class DefaultConfig : INLogConfig
        {
            private readonly NLog.Config.LoggingConfiguration config_;


            public DefaultConfig()
            {

#if DEBUG
                var fileLogLevel = NLog.LogLevel.Trace;
#elif RELEASE
                var fileLogLevel = NLog.LogLevel.Info;
#endif
         
                try
                {
                    // Step 1. Create configuration object
                    var config = new NLog.Config.LoggingConfiguration();
                
                    // Step 2. Create targets and add them to the configuration
                    var consoleTarget = new NLog.Targets.ColoredConsoleTarget();
                    //config.AddTarget("console", consoleTarget);
                   
                    // Step 3. Set target properties
                    consoleTarget.Layout =
"${logger} [${callsite:fileName=False:skipFrames=1}] at " +
"${callsite:fileName=True:includeSourcePath=False:className=False:methodName=False:skipFrames=1}\n" +
"(${date:format=yyyy-MM-dd HH\\:mm\\:ss.ffff}) ${pad:padding=5:inner=${level:uppercase=true}}: ${message}\n";

                    var fileTarget = new NLog.Targets.FileTarget()
                    {
                        Layout = consoleTarget.Layout,
                        ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                        FileName = @"${basedir}/logs/${logger}_${shortdate}.log",
                        KeepFileOpen = true,
                        OpenFileCacheTimeout = 30
                    };
                    // Step 4. Define rules
                    
                    //open async writting
#if DEBUG
                    var consoleLevel = NLog.LogLevel.Trace;
                    var consoleAsyncTarget = new AsyncTargetWrapper(consoleTarget);
                    var rule1 = new NLog.Config.LoggingRule("*", consoleLevel, consoleAsyncTarget);
                    config.LoggingRules.Add(rule1);
#endif
                    var fileAsyncTarget = new AsyncTargetWrapper(fileTarget);
                    var rule2 = new NLog.Config.LoggingRule("*", fileLogLevel, fileAsyncTarget);
                    
                    config.LoggingRules.Add(rule2); 
                    
                    this.config_ = config;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    throw;
                }
            }


            public NLog.Config.LoggingConfiguration Configuration => this.config_;
        }


        private static object Mutex { get; }


        private static DefaultConfig GlobalConfig { get; }


        private static Dictionary<string, ILogger> LoggerDict { get; }


        static DefaultFactory()
        {
            Mutex = new object();
            GlobalConfig = new DefaultConfig();
            LoggerDict = new Dictionary<string, ILogger>();
        }


        public ILogger GetLogger(string loggerName)
        {
            lock (DefaultFactory.Mutex)
            {
                if (!LoggerDict.TryGetValue(loggerName, out ILogger logger))
                {
                    logger = new NLogLogger<DefaultConfig>(
                        DefaultFactory.GlobalConfig,
                        loggerName
                    );

                    LoggerDict[loggerName] = logger;
                }
                return logger;
            }
        }
    }
}
