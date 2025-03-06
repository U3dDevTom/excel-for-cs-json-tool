using System;
using System.IO;
using ThirdParty.Loggings;


namespace ExcelToJsonCs
{
    public static class Program
    {
        public static void Main()
        {
            while (true)
            {
                var appPath = FileHelper.GetApplicationRoot_();
                if (string.IsNullOrEmpty(appPath))
                {
                    var uri = new Uri(typeof(Program).Assembly.Location);
                    appPath = Path.GetDirectoryName(uri.LocalPath);
                }

                if (string.IsNullOrEmpty(appPath))
                {
                    LogService.Logger.Error("can't set output path");
                    throw new Exception();
                }

                Console.WriteLine($"该程序解析的规则如下 " +
                                  $"\n第一行 c/s/cs 代表要输出的对应的客户端或服务器配置或都有.如果==空则统一记录cs" +
                                  $"\n第二行 字段类型.如 int long float array object" +
                                  $"\n第三行 描述解析" +
                                  $"\n第四行 配置字段名称" +
                                  $"\n第五行→结尾 记录每行配置对应的字段值");

                Console.WriteLine($"该程序对默认空的字段值采用对应的 0 或 false");
                Console.WriteLine("输入要输出的配置文件夹.父级即可.不需要带ClientConfigs子文件夹");
                var outputDir = Path.Combine(appPath, "../Configs");
                outputDir = Path.GetFullPath(outputDir);
                
                var tempDir = Console.ReadLine();
                
                if (string.IsNullOrEmpty(tempDir))
                {
                    Console.WriteLine($"将使用默认的输出路径 {outputDir}");
                }
                else
                {
                    try
                    {
                        if (Directory.Exists(tempDir) == false)
                        {
                            Directory.CreateDirectory(tempDir);
                        }
                        outputDir = tempDir;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"路径 {tempDir} 有异常.改用默认路径 {outputDir} {e}");
                    }
                }

                var clientPath = Path.Combine(outputDir, "ClientConfigs");
                var serverPath = Path.Combine(outputDir, "ServerConfigs");

                string excelPaths = null;
                while (true)
                {
                    LogService.Logger.Info("Input excel directory path: \n 输入要解析表的Excel所在文件夹绝对路径,如：E:\\AliceGameDoc\\DesignConfig\\Excels");
                    excelPaths = Console.ReadLine();
                    //pathInput = "E:\\CDiskBackup\\AliceGameDoc\\DesignConfig\\Excels";
                    if (!Directory.Exists(excelPaths))
                    {
                        LogService.Logger.Error($"Not found {excelPaths}");
#if Release
                        Console.WriteLine($"Not found {excelPaths});
#endif
                        continue;
                    }
                    break;
                }
                
                var setting = new ConfigHandlerSetting(excelPaths, clientPath, serverPath);
                Console.WriteLine($"excelPaths path {excelPaths} \n client: {clientPath}\n server: {serverPath}");
                try
                {
                    HandlerAllFilesMode(setting);
                }
                catch (Exception e)
                {
                    LogService.Logger.Error(e);
#if Release
                    Console.WriteLine(e);
#endif
                    
                    break;
                }

                var overMsg = $"Write over \nclient path:{clientPath} \nserver path:{serverPath}";
                LogService.Logger.Info(overMsg);
#if Release
                Console.WriteLine(overMsg);
#endif
                break;
            }
            Console.WriteLine("程序已结束.按任意退出");
            Console.ReadLine();
        }

        
        
        
        
        private static void HandlerAllFilesMode(ConfigHandlerSetting setting)
        {
            ExcelExporter.Export(setting);
        }
    }
}

