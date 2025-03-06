using System;
using ThirdParty.Loggings.Adaptors.NLogBase;

namespace ThirdParty.Loggings
{
    public static class LogService
    {
        private static readonly Lazy<ILogger> lazyLogger_ = new Lazy<ILogger>(() =>
        {
            var factory = new DefaultFactory();
            return factory.GetLogger("Config");
        });


        public static ILogger Logger => lazyLogger_.Value;
    }


    public static class CustomJsonConvertSetting
    {
        private static readonly Lazy<Newtonsoft.Json.JsonSerializerSettings> lazySettings_ =
            new Lazy<Newtonsoft.Json.JsonSerializerSettings>(() =>
            {
                var settings = new Newtonsoft.Json.JsonSerializerSettings();
                settings.Formatting = Newtonsoft.Json.Formatting.Indented;
                return settings;
            });


        public static Newtonsoft.Json.JsonSerializerSettings GetSettings()
            => lazySettings_.Value;
    }


    public static class LoggerJsonifyExt
    {
        public static string Jsonify(this ILogger logger, object objData)
        {
            try
            {
                var jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(
                    value: objData,
                    formatting: Newtonsoft.Json.Formatting.Indented,
                    settings: CustomJsonConvertSetting.GetSettings()
                );
                return string.Format("\n{0}", jsonStr);
            }
            catch(Exception e)
            {
                logger.Error(e);
                return e.Message;
            }
        }


        public static string Jsonify(this object objData)
        {
            try
            {
                var jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(
                    value: objData,
                    formatting: Newtonsoft.Json.Formatting.Indented,
                    settings: CustomJsonConvertSetting.GetSettings()
                );
                return string.Format("\n{0}", jsonStr);
            }
            catch (Exception e)
            {
                var log = LogService.Logger;
                log.Error(e);
                return e.Message;
            }
        }
    }
}
