using System;

namespace ExcelToJsonCs
{
    public class ConfigHandlerSetting
    {
        public readonly string ExcelFileDirPath;


        public readonly string ClientConfigOutputPath;


        public readonly string ServerConfigOutputPath;

        
        public ConfigHandlerSetting(string excelFileDirPath, string clientConfigOutputPath, string serverConfigOutputPath)
        {
            ExcelFileDirPath = excelFileDirPath ?? throw new ArgumentNullException(nameof(excelFileDirPath));
            ClientConfigOutputPath =
                clientConfigOutputPath ?? throw new ArgumentNullException(nameof(clientConfigOutputPath));
            ServerConfigOutputPath =
                serverConfigOutputPath ?? throw new ArgumentNullException(nameof(serverConfigOutputPath));
        }
    }
}