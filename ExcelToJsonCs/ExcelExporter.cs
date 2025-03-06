using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Text;
using OfficeOpenXml;
using ThirdParty.Loggings;

namespace ExcelToJsonCs
{
    public static class ExcelExporter
    {
        public enum OutFileLocale
        {
            Server,
            Client
        }
        
        
        class HeadInfo
        {
            public string FieldCS;
            public string FieldDesc;
            public string FieldName;
            public string FieldType;
            public int FieldIndex;

            public bool C;
            public bool S;
            public HeadInfo(string cs, string desc, string name, string type, int index)
            {
                this.FieldCS = cs;
                this.FieldDesc = desc;
                this.FieldName = name;
                this.FieldType = type;
                this.FieldIndex = index;
                this.C = cs.Contains('c');
                this.S = cs.Contains('s');
            }
        }

        class Table
        {
            public int SheetTableIndex;
            public Dictionary<string, HeadInfo> HeadInfos = new Dictionary<string, HeadInfo>();
        }

        /// <summary>
        /// key table name
        /// </summary>
        private static readonly Dictionary<string, Table> tables = new Dictionary<string, Table>();
        private static readonly Dictionary<string, ExcelPackage> packages = new Dictionary<string, ExcelPackage>();


        private static Table GetTable(string tableName,int index)
        {
            if (!tables.TryGetValue(tableName, out var table))
            {
                table = new Table()
                {
                    SheetTableIndex = index
                };
                tables[tableName] = table;
                LogService.Logger.Info($"add table {tableName}");
            }

            return table;
        }

        public static ExcelPackage GetPackage(string fileFullPath, string fileName)
        {
            if (!packages.TryGetValue(fileName, out var package))
            {
                using Stream stream = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                package = new ExcelPackage(stream);
                packages[fileName] = package;
            }

            return package;
        }


        public static void Export(ConfigHandlerSetting setting)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                if (Directory.Exists(setting.ClientConfigOutputPath))
                {
                    Directory.Delete(setting.ClientConfigOutputPath, true);
                }

                Directory.CreateDirectory(setting.ClientConfigOutputPath);

                if (Directory.Exists(setting.ServerConfigOutputPath))
                {
                    Directory.Delete(setting.ServerConfigOutputPath, true);
                }

                Directory.CreateDirectory(setting.ServerConfigOutputPath);

                List<string> files = FileHelper.GetAllFiles(setting.ExcelFileDirPath);
                //Debug only find one  
                //files = new List<string>() { files[0] };
                int excelCount = 0;
                foreach (var path in files)
                {
                    string fileName = Path.GetFileName(path);

                    if (!(fileName.EndsWith(".xlsm") || fileName.EndsWith(".xlsx")) || fileName.StartsWith("~$") ||
                        fileName.Contains("#"))
                    {
                        continue;
                    }

                    LogService.Logger.Debug($"load Files {fileName} to memory");
                    ExcelPackage p = GetPackage(path, fileName);
                    ExportExcelClass(p);
                    excelCount++;
                }

                if (excelCount == 0)
                {
                    LogService.Logger.Error($"{setting.ExcelFileDirPath} 没有任何符合的excel文件");
                    throw new Exception("path no excels");
                }

                //LogService.Logger.Debug(tables.First().Jsonify());
                LogService.Logger.Info("开始输出服务器配置");
                //输出服务器的配置
                foreach (var kv in packages)
                {
                    var p = kv.Value;
                    foreach (ExcelWorksheet worksheet in p.Workbook.Worksheets)
                    {
                        if (!GetSheetIsEffect(worksheet))
                        {
                            continue;
                        }

                        var tableName = worksheet.Name;
                        var table = GetTable(tableName, worksheet.Index);
                        try
                        {
                            //输出服务器的配置
                            ExportExcelJson(setting, worksheet, tableName, table, OutFileLocale.Server,kv.Key);
                            ExportExcelForCsField(setting.ServerConfigOutputPath, worksheet, tableName, table, OutFileLocale.Server,kv.Key);
                        }
                        catch (Exception e)
                        {
                            LogService.Logger.Error($"table {tableName} error {e}");
                            throw;
                        }
                        //输出客户端配置
                        // ExportExcelJson(clientCsJsonDir, worksheet, tableName, table, OutFileLocale.Client);
                        // ExportExcelForCsField(clientCsJsonDir, worksheet, tableName, table, OutFileLocale.Client);
                    }
                }
                
                OutputCustomObject(setting.ServerConfigOutputPath,OutFileLocale.Server);
                customObjectInfo.Clear();
                ignoreConfigRec.Clear();
                //输出客户端的配置
                
                LogService.Logger.Info("开始输出客户端配置");
                foreach (var kv in packages)
                {
                    var p = kv.Value;
                    foreach (ExcelWorksheet worksheet in p.Workbook.Worksheets)
                    {
                        if (!GetSheetIsEffect(worksheet))
                        {
                            continue;
                        }
                        var tableName = worksheet.Name;
                        var table = GetTable(tableName, worksheet.Index);
                        try
                        {
                            //输出服务器的配置
                            ExportExcelJson(setting, worksheet, tableName, table, OutFileLocale.Client,kv.Key);
                            ExportExcelForCsField(setting.ClientConfigOutputPath, worksheet, tableName, table, OutFileLocale.Client,kv.Key);
                        }
                        catch (Exception e)
                        {
                            LogService.Logger.Error($"file {kv.Key} table {tableName} error {e}");
                            throw;
                        }
                        //输出客户端配置
                        // ExportExcelJson(clientCsJsonDir, worksheet, tableName, table, OutFileLocale.Client);
                        // ExportExcelForCsField(clientCsJsonDir, worksheet, tableName, table, OutFileLocale.Client);
                    }
                }
                OutputCustomObject(setting.ClientConfigOutputPath,OutFileLocale.Client);
            }
            catch (Exception e)
            {
                LogService.Logger.Error(e);
                throw;
            }
            finally
            {
                tables.Clear();
                foreach (var kv in packages)
                {
                    kv.Value.Dispose();
                }

                packages.Clear();
            }
        }


        static void OutputCustomObject(string outputPathDir,OutFileLocale locale)
        {
            foreach (var a in customObjectInfo.Values) //最后输出自定义的字段cs
            {
                if (locale == OutFileLocale.Client)
                {
                    if (a.CustomFields.Any(c=>c.Value.C) == false)
                    {
                        continue;
                    }
                }
                else if (locale == OutFileLocale.Server)
                {
                    if (a.CustomFields.Any(c=>c.Value.S) == false)
                    {
                        continue;
                    }
                }
                

                string outputPath = Path.Combine(outputPathDir, $"{a.TypeName}.cs");
                using var fileStream = File.Open(outputPath, FileMode.OpenOrCreate);
                string oldValue = string.Empty;
                using (StreamReader sr = new StreamReader(fileStream))
                {
                    oldValue = sr.ReadToEnd();
                }
                var contentStart = oldValue.IndexOf("\t{", StringComparison.Ordinal);
                var contentEnd = oldValue.LastIndexOf("\t}", StringComparison.Ordinal);
                var content = oldValue.Substring(contentStart + 1, contentEnd - contentStart);
                var cusSb = GetCsCreateStringBuilder(a.TypeName,locale, false);
                foreach (var split in content.Split('\n'))
                {
                    if (split.Length > 2)
                    {
                        cusSb.AppendLine(split);
                    }
                }

                if (locale == OutFileLocale.Client)
                {
                    foreach (var filed in a.CustomFields.Values.Where(c=>c.C))
                    {
                        var writeLine = $"\t\tpublic {filed.FieldType} {filed.FieldName};";
                        if (content.Contains(writeLine))
                        {
                            continue;
                        }

                        cusSb.AppendLine(writeLine);
                    }
                }
                else if(locale == OutFileLocale.Server)
                {
                    foreach (var filed in a.CustomFields.Values.Where(c=>c.S))
                    {
                        var writeLine = $"\t\tpublic {filed.FieldType} {filed.FieldName};";
                        if (content.Contains(writeLine))
                        {
                            continue;
                        }

                        cusSb.AppendLine(writeLine);
                    }
                }
                

                FillCsEndBuild(cusSb);
                var result = cusSb.ToString();
                if (String.CompareOrdinal(result, oldValue) != 0)
                {
                    using var reWrite = File.Open(outputPath, FileMode.OpenOrCreate);
                    reWrite.Seek(0, SeekOrigin.Begin);
                    reWrite.Flush();
                    reWrite.Seek(0, SeekOrigin.Begin);
                    reWrite.Close();
                    using var swl = new StreamWriter(outputPath, true, Encoding.UTF8);
                    swl.Write(result);
                }
            }
        }


        private static bool GetSheetIsEffect(ExcelWorksheet worksheet)
        {
            var start = worksheet.Cells[1, 1].Text.Trim();
            if (string.IsNullOrEmpty(start))
            {
                return false;
            }

            if (start.Contains("c") == false && start.Contains("s") == false)
            {
                return false;
            }

            return true;
        }


        static void ExportExcelClass(ExcelPackage p)
        {
            foreach (ExcelWorksheet worksheet in p.Workbook.Worksheets)
            {
                if (!GetSheetIsEffect(worksheet))
                {
                    LogService.Logger.Warn($"Table {worksheet.Name} not effect ");
                    continue;
                }
                var table = GetTable(worksheet.Name,worksheet.Index);
                ExportSheetClass(worksheet, table);
            }
        }


        static void ExportSheetClass(ExcelWorksheet worksheet, Table table)
        {
            const int row = 1;
            int index = 0;
            for (int col = 1; col <= worksheet.Dimension.End.Column; ++col)
            {
                index++;
                if (worksheet.Name.StartsWith("#"))
                {
                    continue;
                }
                string fieldType = worksheet.Cells[row + 1, col].Text.Trim();
                if (ConvertTypes.Contains(fieldType) == false)
                {
                    continue;
                }
                
                string fieldName = worksheet.Cells[row + 3, col].Text.Trim();
                if (fieldName == "")
                {
                    continue;
                }

                if (table.HeadInfos.ContainsKey(fieldName))
                {
                    continue;
                }

                string fieldCS = worksheet.Cells[row, col].Text.Trim().ToLower();

                if (string.IsNullOrEmpty(fieldCS))
                {
                    continue;
                }
                if (table.HeadInfos.TryGetValue(fieldName, out var oldClassField))
                {
                    if (oldClassField.FieldCS != fieldCS)
                    {
                        LogService.Logger.Error(
                            $"field cs not same: {worksheet.Name} {fieldName} oldcs: {oldClassField.FieldCS} {fieldCS}");
                    }

                    continue;
                }

                string fieldDesc = worksheet.Cells[row + 2, col].Text.Trim();

                table.HeadInfos[fieldName] = new HeadInfo(fieldCS, fieldDesc, fieldName, fieldType,index);
            }
        }


        private static readonly HashSet<string> ignoreConfigRec = new HashSet<string>();



        static void ExportExcelJson(ConfigHandlerSetting setting, ExcelWorksheet sheet, string filePrefixName, Table table,OutFileLocale locale,string fileName)
        {
            StringBuilder sb = new StringBuilder();
            var exportedDir = locale == OutFileLocale.Client
                ? setting.ClientConfigOutputPath
                : setting.ServerConfigOutputPath;
            sb.AppendLine("{");
            var addCount = ExportSheetJson(sheet, table.HeadInfos, sb,locale);
            if (addCount == 0) //没有需要的
            {
                ignoreConfigRec.Add(fileName);
                //LogService.Logger.Warn($"Excel:{fileName},table {sheet.Name} 没有任意对应的 {locale.ToString()} 设定.该log仅提示.可能会存在符合的情况");
                return;
            }
            sb.AppendLine("}");
            if (!Directory.Exists(exportedDir))
            {
                Directory.CreateDirectory(exportedDir);
            }
            
            string jsonPath = Path.Combine(exportedDir, $"{filePrefixName}.json");
            if (File.Exists(jsonPath))
            {
                using var oldInfo = new FileStream(jsonPath, FileMode.Open);
                var length = oldInfo.Length;
                LogService.Logger.Error($"输出Json文件时错误:has exist files {oldInfo.Name} length {length} sb length {sb.Length} :fileName {fileName}  table:{sheet.Name} 检查配置是否有冲突 ");
                return;
            }
            using FileStream txt = new FileStream(jsonPath, FileMode.Create);
            using StreamWriter sw = new StreamWriter(txt);
            var writeValue = sb.ToString();
            sw.Write(writeValue);
            sw.Close();
            txt.Close();
        }

        /// <summary>
        /// append count
        /// </summary>
        /// <param name="worksheet"></param>
        /// <param name="classField"></param>
        /// <param name="sb"></param>
        /// <param name="fileLocale"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        static int ExportSheetJson(ExcelWorksheet worksheet, Dictionary<string, HeadInfo> classField, StringBuilder sb,OutFileLocale fileLocale)
        {
            int count = 0;
            for (int row = 5; row <= worksheet.Dimension.End.Row; ++row)
            {
                string startKeyId = worksheet.Cells[row, 1].Text.Trim();
                if (startKeyId.Contains("#"))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(startKeyId))
                {
                    continue;
                }
                
                sb.AppendLine($"\t\"{startKeyId}\":{{");

                for (int col = 1; col <= worksheet.Dimension.End.Column; ++col)
                {
                    string fieldName = worksheet.Cells[4, col].Text.Trim();
                    bool isConExist = classField.TryGetValue(fieldName, out var headInfo);
                    
                    if (!isConExist)
                    {
                        continue;
                    }

                    switch (fileLocale)
                    {
                        case OutFileLocale.Server:
                            if (headInfo.S == false)
                            {
                                continue;
                            }
                            break;
                        case OutFileLocale.Client:
                            if (headInfo.C == false)
                            {
                                continue;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(fileLocale), fileLocale, null);
                    }
                    
                    string fieldN = headInfo.FieldName;
                    try
                    {
                        sb.AppendLine(
                            $"\t\t\"{fieldN}\":{ConvertValueToJsonString(headInfo.FieldType, worksheet.Cells[row, col].Text.Trim(),worksheet.Name)},");
                        count++;
                    }
                    catch (Exception e)
                    {
                        LogService.Logger.Error($"{e} \n table {headInfo.Jsonify()} worksheet name {worksheet.Name}");
                        throw;
                    }
                }
                var rmStart = sb.Length - 3;
                sb.Remove(rmStart, 1);
                sb.AppendLine("\t},");
            }

            sb.Remove(sb.Length - 3, 1);
            return count;
        }


        private static readonly HashSet<string> ConvertTypes = new HashSet<string>()
        {
            "bool",
            "uint",
            "int",
            "int32[]",
            "uint32[]",
            "long[]",
            "string[]",
            "int[][]",
            "int32",
            "int64",
            "long",
            "short",
            "float",
            "double",
            "string",
            "array",
            "object"
        };


        private static string ConvertValueToJsonString(string type, string value,string tableName)
        {
            switch (type)
            {
                case "bool":
                {
                    if (value == "1" || value.ToLower() == "true")
                    {
                        return "true";
                    }
                    if(value == "0" || value.ToLower() == "false" || string.IsNullOrEmpty(value))
                    {
                        return "false";
                    }
                    else
                    {
                        throw new Exception($"不支持此类型:\ntype:{type}\n" +
                                            $"value:{value}\n" +
                                            $"parse error");
                    }
                }
                case "uint[]":
                case "int[]":
                case "int32[]":
                case "long[]":
                    return $"[{value}]";
                case "string[]":
                case "int[][]":
                    return $"[{value}]";
                case "int":
                case "uint":
                case "int32":
                case "int64":
                case "long":
                case "short":
                    if (value == "")
                    {
                        return "0";
                    }
                    return value;
                case "float":
                case "double":
                {
                    if (value == "")
                    {
                        return "0";
                    }

                    if (value.Length >1 && value.StartsWith("0"))
                    {
                        var indexOf = value.IndexOf("0.", StringComparison.Ordinal);
                        if (indexOf != 0)
                        {
                            var obj = value;
                            return obj.Remove(0, indexOf);
                        }
                    }

                    return value;
                }
                case "string":
                    value = value.Replace("\\", "\\\\");
                    value = value.Replace("\"", "\\\"");
                    return $"\"{value}\"";
                case "array":
                case "object":
                {
                    if (value.StartsWith("[,"))
                    {
                        LogService.Logger.Error($"配置表 {tableName} 有异常.该字段 {value}.现程序移除第一个 ,");
                        var real = value.Remove(1, 1);
                        return real;
                    }
                    return value;
                }
                default:
                    throw new Exception($"不支持此类型:type:{type} value: {value}");
            }
        }


        private static string GetCsName(string classOrFileName)
        {
            return $"{classOrFileName}_Json";
        }


        static StringBuilder GetCsCreateStringBuilder(string filePrefixName,OutFileLocale locale, bool getCsMethod =true)
        {
            StringBuilder sb = new StringBuilder();
            if (locale == OutFileLocale.Server)
            {
                var csName = getCsMethod ? GetCsName(filePrefixName) : filePrefixName;
                sb.AppendLine("namespace GameJsonModel");
                sb.AppendLine("{");
                sb.AppendLine("\tusing System;");
                sb.AppendLine("\tusing System.Collections.Generic;");
                sb.AppendLine("\t[Serializable]");
                sb.AppendLine($"\tpublic class {csName}");
            }
            else
            {
                var csName = getCsMethod ? GetCsName(filePrefixName) : filePrefixName;
                if (getCsMethod) //json的文件
                {
                    sb.AppendLine("namespace Game.Data");
                    sb.AppendLine("{");
                    sb.AppendLine("\tusing System;");
                    sb.AppendLine("\tusing System.Collections.Generic;");
                    sb.AppendLine("\t[Serializable]");
                    sb.AppendLine($"\t[FileName(\"{filePrefixName}\")]");
                    sb.AppendLine($"\tpublic partial class {csName}:ConfigDataBase<{csName}>"); //客户端配置的的定义
                }
                else //自定义的object
                {
                    sb.AppendLine("namespace Game.Data");
                    sb.AppendLine("{");
                    sb.AppendLine("\tusing System;");
                    sb.AppendLine("\tusing System.Collections.Generic;");
                    sb.AppendLine("\t[Serializable]");
                    sb.AppendLine($"\tpublic class {csName}");
                }
            }

            sb.AppendLine("\t{");
            return sb;
        }


        static void FillCsEndBuild(StringBuilder sb)
        {
            sb.AppendLine("\t}");
            sb.AppendLine("}");
        }
        
        
        static void ExportExcelForCsField(string exportedDir, ExcelWorksheet sheet, string filePrefixName, Table table,OutFileLocale locale,string fileName)
        {
            var sb = GetCsCreateStringBuilder(filePrefixName,locale);
            var count = ExportExcelForCsField(sheet, table.HeadInfos, sb,locale);
            if (count == 0)
            {
                if (ignoreConfigRec.Contains(fileName) == false)
                {
                    LogService.Logger.Error($"Excel {fileName} ,table {sheet.Name} 没有对应 {locale.ToString()} 需要输出的cs文件.并且json文件输出有内容");
                }
                return;
            }
            FillCsEndBuild(sb);
            
            string outputPath = Path.Combine(exportedDir, $"{GetCsName(filePrefixName)}.cs");
            if (!Directory.Exists(exportedDir))
            {
                Directory.CreateDirectory(exportedDir);
            }

            if (File.Exists(outputPath))
            {
                using var oldInfo = new FileStream(outputPath, FileMode.Open);
                var length = oldInfo.Length;
                LogService.Logger.Error($"输出cs文件时错误: has exist files {oldInfo.Name} length {length} sb length {sb.Length} :fileName {fileName}  table:{sheet.Name} 检查配置是否有冲突 ");
                return;
            }
            using FileStream txt = new FileStream(outputPath, FileMode.Create);
            using StreamWriter sw = new StreamWriter(txt);
            sw.Write(sb.ToString());
            sw.Close();
            txt.Close();
        }

        static int ExportExcelForCsField(ExcelWorksheet worksheet, Dictionary<string, HeadInfo> classField, StringBuilder sb,OutFileLocale fileLocale)
        {
            int count = 0;
            if (fileLocale == OutFileLocale.Server)
            {
                foreach (var c in classField)
                {
                    var head = c.Value;
                    if (head.S == false)
                    {
                        continue;
                    }

                    if (head.FieldType == "array")
                    {
                        var typeName = CheckGeneraCustomObject(worksheet, head);
                        sb.AppendLine($"\t\tpublic {typeName}[] {head.FieldName};");
                    }
                    else if(head.FieldType == "object")
                    {
                        var typeName = CheckGeneraCustomObject(worksheet, head);
                        sb.AppendLine($"\t\tpublic {typeName} {head.FieldName};");
                    }
                    else
                    {
                        sb.AppendLine($"\t\tpublic {head.FieldType} {head.FieldName};");
                    }

                    count++;
                }
            }
            else if (fileLocale == OutFileLocale.Client)
            {
                foreach (var c in classField)
                {
                    var head = c.Value;
                    if (head.C == false)
                    {
                        continue;
                    }

                    if (head.FieldType == "array")
                    {
                        var typeName = CheckGeneraCustomObject(worksheet, head);
                        sb.AppendLine($"\t\tpublic {typeName}[] {head.FieldName};");
                    }
                    else if(head.FieldType == "object")
                    {
                        var typeName = CheckGeneraCustomObject(worksheet, head);
                        sb.AppendLine($"\t\tpublic {typeName} {head.FieldName};");
                    }
                    else
                    {
                        sb.AppendLine($"\t\tpublic {head.FieldType} {head.FieldName};");
                    }

                    count++;
                }
            }

            return count;
        }
        
        
        /// <summary>
        /// 获取简单类型或简单数组类型检查
        /// </summary>
        /// <returns></returns>
        public static Queue<(string, Func<string, bool>)> GetSimpleTypesQueueCheck()
        {
            var allType = new Queue<(string,Func<string,bool>)>();
            allType.Enqueue(("bool",(a)=> a.Replace('[',' ').Replace(']',' ').Trim().Split(',').All(c=> bool.TryParse(c,out _))));
            allType.Enqueue(("int",(a)=> a.Replace('[',' ').Replace(']',' ').Trim().Split(',').All(c=> int.TryParse(c,out _))));
            allType.Enqueue(("long",(a)=> a.Replace('[',' ').Replace(']',' ').Trim().Split(',').All(c=> long.TryParse(c,out _))));
            allType.Enqueue(("float",(a)=> a.Replace('[',' ').Replace(']',' ').Trim().Split(',').All(c=> float.TryParse(c,out _))));
            allType.Enqueue(("double",(a)=> a.Replace('[',' ').Replace(']',' ').Trim().Split(',').All(c=> double.TryParse(c,out _))));
            allType.Enqueue(("string",GetIsAllString));
            return allType;
        }


        private static bool GetIsAllString(string a)
        {
            //a.Replace('[', ' ').Replace(']', ' ').Trim().Split(',').All(c => c.Any() && c[0] == '"' && c[^1] == '"');
            if (a.StartsWith('['))
            {
                return a.Substring(0, 2) == "[\"" &&
                       a.Substring(a.Length - 2, 2) == "\"]";
            }
            else
            {
                return a[0] == '\"' && a[a.Length - 1] == '\"';
            }
        }


        private static string GetSimpleAryTypeName(ExcelWorksheet worksheet, HeadInfo classField)
        {
            var queue = GetSimpleTypesQueueCheck();
            var allJudgeCount = queue.Count;
            string simpleType = string.Empty;
            int typeEmptyCount = 0;
            while (queue.Count > 0)
            {
                var tuple = queue.Dequeue();
                bool allSucc = true;
                int count = 0;
                int emptyCount = 0;
                string failedFiled = string.Empty; //Debug用
                int emptyRow = 0;
                for (int row = 5; row <= worksheet.Dimension.End.Row; ++row)
                {
                    var fieldValue = worksheet.Cells[row, classField.FieldIndex].Text.Trim();
                    if (string.IsNullOrEmpty(fieldValue))
                    {
                        emptyRow = row;
                        continue;
                    }

                    var realValue = ConvertValueToJsonString(classField.FieldType, fieldValue, worksheet.Name);
                    var value = realValue.Replace('[', ' ').Trim().Replace(']', ' ').Trim();
                    if (string.IsNullOrEmpty(value))
                    {
                        emptyCount++;
                        continue;
                    }
                    //如果上一列没有数据的.不判定 可能配置有误会导致数据类型有问题
                    if (row - 1 == emptyRow) 
                    {
                        if (count > 0)
                        {
                            LogService.Logger.Error($"table:{worksheet.Name} {classField.FieldName} {row-1} is empty but {row} = {fieldValue} is not null.Choice {tuple.Item1} type");
                        }
                        else
                        {
                            LogService.Logger.Error($"table:{worksheet.Name} {classField.FieldName} {row-1} is empty but {row} = {fieldValue} is not null.maybe get field type error");
                        }

                        break;
                    }
                    count++;
                    if (tuple.Item2(realValue) == false)
                    {
                        allSucc = false;
                        failedFiled = realValue;
                        break;
                    }
                }

                if (allSucc && count > 0)
                {
                    return tuple.Item1;
                }
                if (emptyCount == worksheet.Dimension.End.Row - 4)
                {
                    typeEmptyCount++;
                }
            }

            if (typeEmptyCount == allJudgeCount)
            {
                return "int";
            }

            return simpleType;
        }


        /// <summary>
        /// 记录object或数组的类型
        /// </summary>
        /// <param name="worksheet"></param>
        /// <param name="classField"></param>
        /// <returns></returns>
        static string CheckGeneraCustomObject(ExcelWorksheet worksheet, HeadInfo classField)
        {
            if (classField.FieldType != "object")
            {
                var simpleReturn = GetSimpleAryTypeName(worksheet, classField);
                if (string.IsNullOrEmpty(simpleReturn) == false)
                {
                    return simpleReturn;
                }
            }

            var customObjectTypeName = GetCsName(worksheet.Cells[4, classField.FieldIndex].Text.Trim()); //object typeName
            var some = GetCustomCsInfo(customObjectTypeName);
            for (int row = 5; row <= worksheet.Dimension.End.Row; ++row)
            { 
                var value = worksheet.Cells[row, classField.FieldIndex].Text.Trim();
                try
                {
                    FillCsObjectOrArrayType(some,value, worksheet, classField);
                }
                catch (Exception)
                {
                    LogService.Logger.Error($"table name {worksheet.Name} row {row} FieldName {classField.FieldName} index {classField.FieldIndex}.Field value\n" +
                                            $"{value}");
                    throw;
                }
                
            }
            return customObjectTypeName;
        }


        static CustomClassInfo GetCustomCsInfo(string typeName)
        {
            if(!customObjectInfo.TryGetValue(typeName,out var some))
            {
                some = new CustomClassInfo()
                {
                    CustomFields = new Dictionary<string, CsFieldConfigInfo>(),
                    TypeName = typeName
                };
                customObjectInfo.Add(typeName,some);
            }

            return some;
        }


        static string FindObjectContent(string self)
        {
            const char start = '{';
            const char end = '}';
            try
            {
                var startIndexs = new Queue<int>();
                var endIndexs = new Queue<int>();
                for (int i = 0; i < self.Length; i++)
                {
                    if (self[i] == start)
                    {
                        startIndexs.Enqueue(i);
                    }
                    else if (self[i] == end)
                    {
                        endIndexs.Enqueue(i);
                    }
                }

                int lastEnd = 0;
                while (startIndexs.Count > 0)
                {

                    startIndexs.Dequeue();
                    lastEnd = endIndexs.Dequeue();

                }

                return self.Substring(1, lastEnd - 1);
            }
            catch (Exception e)
            {
                LogService.Logger.Error($"error {e} \n {self}");
                throw;
            }
        }

        const char aryStart = '[';
        const char aryEnd = ']';

        static string FindArrayContent(string self)
        {
            try
            {
                var startIndexs = new Queue<int>();
                var endIndexs = new Queue<int>();
                for (int i = 0; i < self.Length; i++)
                {
                    if (self[i] == aryStart)
                    {
                        startIndexs.Enqueue(i);
                    }
                    else if (self[i] == aryEnd)
                    {
                        endIndexs.Enqueue(i);
                    }
                }

                int lastEnd = 0;
                while (startIndexs.Count > 0)
                {

                    startIndexs.Dequeue();
                    lastEnd = endIndexs.Dequeue();

                }

                return self.Substring(1, lastEnd - 1);
            }
            catch (Exception e)
            {
                LogService.Logger.Error($"error {e} \n {self}");
                throw;
            }
        }


        /// <summary>
        /// 获取该字段中最接近的字段类型.调用自己时要确保classInfo是对应的类型
        /// </summary>
        /// <param name="classInfo">自定义的类型命名合内置字段信息</param>
        /// <param name="value">数组或object的string内容</param>
        /// <param name="worksheet"></param>
        /// <param name="classField"></param>
        /// <returns></returns>
        static void FillCsObjectOrArrayType(CustomClassInfo classInfo,string value, ExcelWorksheet worksheet, HeadInfo classField)
        {
            //这里开头的值应该是数组或Object
            if (value.StartsWith('['))
            {
                //数据开头
                var findContent = FindArrayContent(value); //数组内容
                if (findContent.StartsWith("{")) //是自定义的数值内容 拆分每个object的内容
                {
                    if (findContent.Length <= 2)
                    {
                        return;//没有内容
                    }

                    var firstIndex = findContent.IndexOf(':');
                    var preStart = findContent.Substring(0, firstIndex);
                    var split = findContent.Split(preStart);

                    for (int i = 1; i < split.Length; i++)
                    {
                        var tempValue = split[i];
                        if (i + 1 < split.Length)
                        {
                            tempValue = $"{preStart}{tempValue.Remove(tempValue.Length-1)}";
                        }
                        else
                        {
                            tempValue = $"{preStart}{tempValue}";
                        }

                        try
                        {
                            FillPeerFieldType(classInfo, tempValue, worksheet, classField);
                        }
                        catch (Exception e)
                        {
                            LogService.Logger.Error($"{e} error value\n{tempValue}");
                            throw;
                        }
                        
                    }
                    
                    // var prefix = value.Split("},")[0];
                    // for (int i = 0; i < split.Length; i++)
                    // {
                    //     var tempValue = split[i];
                    //     if (i + 1 < split.Length)
                    //     {
                    //         tempValue += "}";
                    //     }
                    // }
                }
            }
            else if (value.StartsWith('{')) // object
            {
                if (value.Length <= 2)
                {
                    return;//没有内容
                }

                var firstIndex = value.IndexOf(':');
                var preStart = value.Substring(0, firstIndex);
                var split = value.Split(preStart);

                for (int i = 1; i < split.Length; i++)
                {
                    var tempValue = split[i];
                    if (i + 1 < split.Length)
                    {
                        tempValue = $"{preStart}{tempValue.Remove(tempValue.Length-1)}";
                    }
                    else
                    {
                        tempValue = $"{preStart}{tempValue}";
                    }

                    try
                    {
                        FillPeerFieldType(classInfo, tempValue, worksheet, classField);
                    }
                    catch (Exception e)
                    {
                        LogService.Logger.Error($"{e} error value\n{tempValue}");
                        throw;
                    }
                        
                }
                    
                // var prefix = value.Split("},")[0];
                // for (int i = 0; i < split.Length; i++)
                // {
                //     var tempValue = split[i];
                //     if (i + 1 < split.Length)
                //     {
                //         tempValue += "}";
                //     }
                // }
            }
            else
            {
                //应该不存在到这里 或者是空的内容
            }
        }


        static void FillPeerFieldType(CustomClassInfo classInfo ,string objectValue,ExcelWorksheet worksheet,HeadInfo classField)
        {
            int index = 0;
            char filedStart = ' ';
            int rowSplit = -1;
            var setValue = objectValue.ToString();
            
            try
            {
                if (objectValue.StartsWith('{')) //剔除外框 留下字段
                {
                    setValue = FindObjectContent(objectValue);
                }

                index = setValue.IndexOf(':');
                if (index == -1) //没有字段了
                {
                    return;
                }

                rowSplit = setValue.IndexOf(',');
                filedStart = setValue[index + 1];
            }
            catch (Exception e)
            {
                LogService.Logger.Error(e);
                throw;
            }
            
            var firstFiledName = setValue.Substring(1, index - 2);//-“” 
            if (rowSplit == -1) //只有一行
            {
                if (filedStart == '{') //有新的对象内容 
                {
                    var newTypeName = GetCsName($"{classInfo.TypeName}_{firstFiledName}");
                    var filedInfo = new CsFieldConfigInfo(fieldName: firstFiledName, type: $"{newTypeName}",classField.C,classField.S);
                    classInfo.AddOrReplace(filedInfo); //把该字段加入到原本的类型中
                    //截图该object的字段内容 
                    var remainAll = setValue.Substring(index + 1, setValue.Length - index - 1);
                    var currentObjectContent = FindObjectContent(remainAll);
                    var some = GetCustomCsInfo(newTypeName);
                    FillPeerFieldType(some, currentObjectContent, worksheet, classField);
                }
                else if (filedStart == '[')
                {
                    if (setValue[index + 2] == '{')
                    {
                        //新对象
                        var newTypeName = GetCsName($"{classInfo.TypeName}_{firstFiledName}Ary");
                        var filedInfo = new CsFieldConfigInfo(fieldName: firstFiledName, type: $"{newTypeName}[]",classField.C,classField.S);
                        classInfo.AddOrReplace(filedInfo); //把该字段加入到原本的类型中
                        //截图该数组的字段内容 相当于重新走一次FillCsObjectOrArrayType 
                        var some = GetCustomCsInfo(newTypeName);
                        var remainAll = setValue.Substring(index + 1, setValue.Length - index - 1);
                        FillCsObjectOrArrayType(some, remainAll, worksheet, classField);
                    }
                    else
                    {
                        //普通数组 
                        var findIndexFirstIndex = setValue.IndexOf(']');
                        var fieldValue = setValue.Substring(index + 1, findIndexFirstIndex - index);
                        FillCsFieldValueType(classInfo, firstFiledName, fieldValue, worksheet, classField);
                        var remainAll = setValue.Substring(index + 1, setValue.Length - index - 1);
                        //检查一下是否还有剩余的内容
                        var remainValue = remainAll.Replace(fieldValue,"").Trim();
                        if (remainValue.Length > 0)
                        {
                            if (remainValue[0] == ',')
                            {
                                FillPeerFieldType(classInfo, remainValue.Remove(0, 1), worksheet, classField);
                            }
                        }
                    }
                }
                else
                {
                    var filedValue = setValue.Substring(index + 1, setValue.Length - index - 1);
                    FillCsFieldValueType(classInfo, firstFiledName, filedValue, worksheet, classField);
                }
            }
            else //有多行 可能第一行就有数组
            {
                if (filedStart == '{') //有新的对象内容 
                {
                    var newTypeName = GetCsName($"{classInfo.TypeName}_{firstFiledName}");
                    var filedInfo = new CsFieldConfigInfo(fieldName: firstFiledName, type: $"{newTypeName}",classField.C,classField.S);
                    classInfo.AddOrReplace(filedInfo); //把该字段加入到原本的类型中
                    
                    //截图该object的字段内容 
                    var remainAll = setValue.Substring(index + 1, setValue.Length - index - 1);
                    var currentRemainIndex = remainAll.IndexOf("},", StringComparison.Ordinal);
                    int splitTimes = 0;
                    while (true)
                    {
                        if (currentRemainIndex == -1)
                        {
                            if (splitTimes == 0)
                            {
                                var some = GetCustomCsInfo(newTypeName);
                                FillPeerFieldType(some, remainAll, worksheet, classField);
                            }
                            else //splitTimes >0 时说明是同一个字段下的.
                            {
                                FillPeerFieldType(classInfo, remainAll, worksheet, classField); 
                            }

                            break;
                        }
                        else
                        {
                            var interContent = remainAll.Substring(0, currentRemainIndex + 1);
                            var some = GetCustomCsInfo(newTypeName);
                            FillPeerFieldType(some, interContent, worksheet, classField);
                            remainAll = remainAll.Substring(currentRemainIndex + 2,
                                remainAll.Length - currentRemainIndex - 2);
                            currentRemainIndex = remainAll.IndexOf("},", StringComparison.Ordinal);
                            splitTimes++;
                        }
                    }
                    
                }
                else if(filedStart == '[') //数组
                {
                    if (setValue[index + 2] == '{')
                    {
                        //新对象
                        var newTypeName = GetCsName($"{classInfo.TypeName}_{firstFiledName}Ary");
                        var filedInfo = new CsFieldConfigInfo(fieldName: firstFiledName, type: $"{newTypeName}[]",classField.C,classField.S);
                        classInfo.AddOrReplace(filedInfo); //把该字段加入到原本的类型中
                        
                        //截图该数组的字段内容 相当于重新走一次FillCsObjectOrArrayType 
                        var some = GetCustomCsInfo(newTypeName);
                        var remainAll = setValue.Substring(index + 1, setValue.Length - index - 1);
                        FillCsObjectOrArrayType(some, remainAll, worksheet, classField);
                    }
                    else
                    {
                        //普通数组 

                        var waitArrayValue = setValue;
                        
                        var findIndexFirstIndex = waitArrayValue.IndexOf(']');
                        var fieldValue = waitArrayValue.Substring(index + 1, findIndexFirstIndex - index);
                        FillCsFieldValueType(classInfo, firstFiledName, fieldValue, worksheet, classField);

                        //检查一下是否还有剩余的内容
                        if (findIndexFirstIndex + 1 == waitArrayValue.Length)
                        {
                            return;
                        }
                        
                        var remainValue = waitArrayValue.Substring(findIndexFirstIndex+1,waitArrayValue.Length - findIndexFirstIndex -1);

                        if (remainValue.Length > 0)
                        {
                            if (remainValue[0] == ',')
                            {
                                var rePush = $"{{{remainValue.Remove(0, 1)}}}";
                                
                                try
                                {
                                    FillPeerFieldType(classInfo,rePush, worksheet, classField);
                                }
                                catch (Exception e)
                                {
                                    LogService.Logger.Error(e);
                                    throw;
                                }
                            }
                        }
                    }
                }
                else //这里间隔的字段
                {
                    if (filedStart == '"')
                    {
                        var lineIndex = setValue.IndexOf("\",", StringComparison.Ordinal);
                        if (lineIndex != -1)
                        {
                            var fieldValue = setValue.Substring(index + 1, lineIndex - index);
                            FillCsFieldValueType(classInfo, firstFiledName, fieldValue, worksheet, classField);
                            var remainAll = setValue.Substring(lineIndex + 2, setValue.Length - lineIndex - 2); 
                            FillPeerFieldType(classInfo, remainAll, worksheet, classField);
                        }
                        else
                        {
                            var fieldValue = setValue.Substring(index + 1, rowSplit - index - 1);
                            FillCsFieldValueType(classInfo, firstFiledName, fieldValue, worksheet, classField);
                            var remainAll = setValue.Substring(rowSplit + 1, setValue.Length - rowSplit - 1); 
                            FillPeerFieldType(classInfo, remainAll, worksheet, classField);
                        }
                    }
                    else
                    {
                        var fieldValue = setValue.Substring(index + 1, rowSplit - index - 1);
                        FillCsFieldValueType(classInfo, firstFiledName, fieldValue, worksheet, classField);
                        var remainAll = setValue.Substring(rowSplit + 1, setValue.Length - rowSplit - 1); 
                        FillPeerFieldType(classInfo, remainAll, worksheet, classField);
                    }
                }
            }
        }


        static void FillCsFieldValueType(CustomClassInfo classInfo,string fieldName ,string fieldValue,ExcelWorksheet worksheet, HeadInfo classField)
        {
            var realValue = ConvertValueToJsonString(classField.FieldType, fieldValue, worksheet.Name);
            if (fieldValue.Length > 0)
            {
                if (fieldValue[0] == '"') //string
                {
                    var some = new CsFieldConfigInfo(fieldName, "string",classField.C,classField.S);
                    classInfo.AddOrReplace(some);
                }
                else if (fieldValue[0] == '{') // object 
                {
                    var newTypeName = GetCsName($"{classInfo.TypeName}_{fieldName}");
                    var some = GetCustomCsInfo(newTypeName);
                    var info = new CsFieldConfigInfo(fieldName, newTypeName,classField.C,classField.S);
                    some.AddOrReplace(info);
                    FillCsObjectOrArrayType(some, fieldValue, worksheet, classField);
                }
                else if (fieldValue[0] == '[') //数组
                {
                    var queue = GetSimpleTypesQueueCheck();
                    string simpleType = string.Empty;
                    while (queue.Count > 0)
                    {
                        var tuple = queue.Dequeue();
                        
                        if (tuple.Item2(realValue))
                        {
                            simpleType = tuple.Item1;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(simpleType) == false)
                    {
                        var info = new CsFieldConfigInfo(fieldName, $"{simpleType}[]",classField.C,classField.S);
                        classInfo.AddOrReplace(info);
                    }
                }
                else
                {
                    var queue = GetSimpleTypesQueueCheck();
                    string simpleType = string.Empty;
                    while (queue.Count > 0)
                    {
                        var tuple = queue.Dequeue();
                        if (tuple.Item2(realValue))
                        {
                            simpleType = tuple.Item1;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(simpleType) == false)
                    {
                        var info = new CsFieldConfigInfo(fieldName, $"{simpleType}",classField.C,classField.S);
                        classInfo.AddOrReplace(info);
                    }
                    else
                    {
                        LogService.Logger.Error($"can't case {fieldValue} to simple type");
                        throw new Exception();
                    }
                }
            }
        }
        
        
        
        
        
        


        /// <summary>
        /// key = internal property type Name
        /// </summary>
        private static readonly Dictionary<string, CustomClassInfo> customObjectInfo = new Dictionary<string, CustomClassInfo>();
        
    }
}