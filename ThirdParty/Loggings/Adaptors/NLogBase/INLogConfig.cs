
namespace ThirdParty.Loggings.Adaptors.NLogBase
{
    public interface INLogConfig
    {
        NLog.Config.LoggingConfiguration Configuration { get; }
    }
}
