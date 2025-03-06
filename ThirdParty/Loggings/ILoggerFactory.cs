namespace ThirdParty.Loggings
{
    public interface ILoggerFactory
    {
        ILogger GetLogger(string loggerName);
    }
}
