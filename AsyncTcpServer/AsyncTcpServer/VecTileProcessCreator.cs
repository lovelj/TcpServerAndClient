using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Geoway.ADF.GDC.DatasetUI.Private;
using Geoway.ADF.MIS.Utility.Log;
using Geoway.ADF.MIS.Utility.Tools;
using Newtonsoft.Json.Linq;

namespace Geoway.ADF.AsyncTcpServer
{
    /*
     *接收消息json格式
{
    "processtype": "vectile",
    "db": {
        "dbtype": "",
        "url": "",
        "username": "",
        "password": ""
    },
    "table": {
        "layername": "",
        "tablename": "",
        "indexname": "",
        "srid": "",
        "keyfield": "",
        "shapefield": "",
        "selectfields": "",
        "bbox": "",
        "levels": "",
        "defaultstyle": ""
    },
    "threads": {
        "count": "4"
    }
}
     */
    /// <summary>
    /// 矢量瓦片创建类 
    /// </summary>
    class VecTileProcessCreator
    {
        string PATH_TOOL = @"D:\tomcat7\webapps\mapserver";
        string PATH_Config = @"D:\tomcat7\webapps\mapserver\WEB-INF\classes\conf";
        string FILE_NODEJS = @"D:\tomcat7\webapps\mapserver\server\run.bat";
        string FILE_TOMCAT = @"D:\tomcat7\bin\startup.bat";
        string FILE_TILE = @"D:\tomcat7\webapps\mapserver\WEB-INF\runCuter.bat";

        private string keynameTool = "VECTILETOOLPATH";
        private string keynameConf = "VECTILECONFPATH";
        private string keynameNodejs = "VECTILENODEJSPATH";
        private string keynameTomcat = "VECTILETOMCATPATH";
        private string keynameTileCuter = "VECTILETILECUTERPATH";
        private LogInstance logInstance = null;
       
        public VecTileProcessCreator()
        {

            PATH_TOOL = GetConfigValue(keynameTool, @"D:\tomcat7\webapps\mapserver");
            PATH_Config = GetConfigValue(keynameConf, @"D:\tomcat7\webapps\mapserver\WEB-INF\classes\conf");
            FILE_NODEJS = GetConfigValue(keynameNodejs, @"D:\tomcat7\webapps\mapserver\server\run.bat");
            FILE_TOMCAT = GetConfigValue(keynameTomcat, @"D:\tomcat7\bin\startup.bat");
            FILE_TILE = GetConfigValue(keynameTileCuter, @"D:\tomcat7\webapps\mapserver\WEB-INF\runCuter.bat");
            string path = Path.Combine("vectile", DateTime.Now.Month.ToString(), DateTime.Now.Day.ToString());
            string path2 = Path.Combine(DateTime.Now.ToString("HHmmss"));
            logInstance = new LogInstance("vectile", path, true, true);
            logInstance.LogFileFullName = Path.Combine(Path.GetDirectoryName(LogHelper.Error.FilePath), path, "info.txt");
        }

        public bool CreateIndex(string vectileinfo,out string message)
        {
            message = string.Empty;
            string dbtype = string.Empty;
            string dburl = string.Empty;
            string dbusername = string.Empty;
            string dbpassword = string.Empty;

            try
            {
                JObject obj = JObject.Parse(vectileinfo);
                if (obj["processtype"].ToString().Equals("vectile"))
                {
                    JObject dbData = obj["db"] as JObject;
                    JObject tableData = obj["table"] as JObject;
                    JObject threadData = obj["threads"] as JObject;
                    int threadcount=int.Parse(threadData["count"].ToString());
                    int srid = int.Parse(tableData["srid"].ToString());

                    var configFile = GenerateTileConfigFile(tableData["layername"].ToString(), tableData["tablename"].ToString(), tableData["indexname"].ToString(),
                                                        tableData["keyfield"].ToString(), tableData["shapefield"].ToString(), tableData["selectfields"].ToString(), tableData["levels"].ToString()
                                                        , tableData["bbox"].ToString(), srid, threadcount);
                    #region 
                    //检查矢量瓦片工具
                    if (!Directory.Exists(PATH_TOOL))
                    {
                        message = "矢量瓦片工具不存在，请先部署工具！";
                        return false;
                    }

                    //1.关闭tomcat、nodejs
                    CloseTools();

                    //2.设置切片配置文件
                    if (!SetTileConfigFile(configFile))
                    {
                        message = "配置文件生成失败！";
                        return false;
                    }

                    //3.修改数据库配置文件
                    if (!UpdateDatabaseConfig(dbData["dbtype"].ToString(), dbData["url"].ToString(), dbData["username"].ToString(), dbData["password"].ToString()))
                    {
                        message = "数据库文件配置失败！";
                        return false;
                    }

                    //4.开始切片
                    string tilemessge=string.Empty;
                    if (!StartTile(out tilemessge))
                    {
                        message = tilemessge;
                        return false;
                    }

                    //5.更新配置文件
                    UpdateServiceConfigFile(configFile);

                    //6.启动tomcat
                    StartService();

                    message = string.Empty;
                    return true;
                    #endregion
                }
                else
                {
                    message = "processtype错误";
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error.Append(ex);
                message = ex.Message;
                return false;
            }
            return true;
        }
        //
        public VectorTileConfig GenerateTileConfigFile(string layername, string tableName, string indexName, string oidfield, string shapefield, string selfields, string levels,string extent, int srid, int threadcount)
        {
            try
            {                
                VectorTileConfig configFile = new VectorTileConfig();
                configFile.ThreadCount = threadcount;

                string oidField = oidfield;
                string shapeField = shapefield;  
                VectorTileLayer layer = new VectorTileLayer();
                layer.TableName = tableName;
                layer.IndexName = indexName;
                layer.Fields = selfields;
                layer.OIDField = oidField;
                layer.ShapeField = shapeField;
                layer.Extent = extent;// "-180,-90,180,90";
                layer.Levels = levels;

                layer.Srid = srid;
                configFile.Layers.Add(layer);

                return configFile;
            }
            catch (Exception ex)
            {
                LogHelper.Error.Append(ex);
                return null;
            }
        }

        #region setting
        public bool SetTileConfigFile(VectorTileConfig configFile)
        {
            try
            {
                string tileConfig = Path.Combine(PATH_Config, "TileConfig.xml");
                //备份配置文件
                if (File.Exists(tileConfig))
                {
                    File.Copy(tileConfig, Path.Combine(PATH_Config, "TileConfig_service.xml"), true);
                }

                string str = SerializeUtil.XmlSerialize(configFile);
                SerializeUtil.XmlSerialize(configFile, tileConfig);

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error.Append(ex);
                return false;
            }
        }
        /// <summary>
        /// 设置矢量瓦片数据库连接参数
        /// </summary>
        /// <param name="dbHelper"></param>
        /// <returns></returns>
        public bool UpdateDatabaseConfig(string dbtype,string url,string username,string password)
        {
            try
            {
                string dbConfigFile = string.Empty;
                string prouser = string.Empty;
                string propass = string.Empty;
                string prourl = string.Empty;

                if (dbtype.Equals("postgresql"))
                {
                    dbConfigFile = Path.Combine(PATH_Config, "postgreSQL_database.properties");
                    prourl = "jdbc.url=jdbc:postgresql://" + url;
                    prouser = "jdbc.username=" + username;
                    propass = "jdbc.password=" + password; 
                }
                else if (dbtype.Equals("oracle"))
                {
                    dbConfigFile = Path.Combine(PATH_Config, "database.properties");

                    prourl = "jdbc.url=jdbc:oracle:thin:@" + url;
                        //string.Format("{0}:{1}/{2}", dbHelper.DBServer, string.IsNullOrEmpty(dbHelper.DBPort) ? "1521" : dbHelper.DBPort, dbHelper.DBName);
                    prouser = "jdbc.username=" + username;
                    propass = "jdbc.password=" + password;
                }                

                if (File.Exists(dbConfigFile))
                {
                    string[] lines = File.ReadAllLines(dbConfigFile);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("jdbc.url"))
                        {
                            lines[i] = prourl;
                        }
                        else if (lines[i].StartsWith("jdbc.username"))
                        {
                            lines[i] = prouser;
                        }
                        else if (lines[i].StartsWith("jdbc.password"))
                        {
                            lines[i] = propass;
                        }
                    }

                    File.WriteAllLines(dbConfigFile, lines);
                    return true;
                }
                else
                {
                    throw new Exception("未找到数据库配置文件！");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error.Append(ex);
                return false;
            }
        }
        #endregion

        private bool StartTile(out string message)
        {
            //启动nodejs
            Process nodejsProcess = new Process();
            nodejsProcess.StartInfo.FileName = FILE_NODEJS;
            nodejsProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(FILE_NODEJS);
            nodejsProcess.Start();

            message = "";
            //开始切片
            string Result = string.Empty;
            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = FILE_TILE;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(FILE_TILE);
                proc.StartInfo.RedirectStandardOutput = true;
               
                proc.Start();
                // 异步获取命令行内容  
                proc.BeginOutputReadLine();  
                proc.OutputDataReceived += new DataReceivedEventHandler(proc_OutputDataReceived);
                //Result = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (tileResult)
                {
                    return true;
                }
                else
                {
                    message = errormessage;
                    return false;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
            return true;
        }
        string errormessage = string.Empty;
        bool tileResult = false;
        private void proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (String.IsNullOrEmpty(e.Data) == false)
            {
                
                Console.WriteLine(e.Data);
                logInstance.Append(e.Data);
                if (e.Data.ToLower().Contains("error") || e.Data.Contains("错误"))//|| e.Data.Contains("出错"))
                {
                    CloseTool("java");
                    errormessage = e.Data;
                }
                                
                if (e.Data.Contains("切片完成"))
                {
                    CloseTool("java");
                    tileResult=true;
                }
            }
            else
            {
 
            }
        }

        private void StartService()
        {
            //启动tomcat
            Process proc = new Process();
            proc.StartInfo.FileName = FILE_TOMCAT;
            proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(FILE_TOMCAT);

            proc.Start();

            System.Threading.Thread.Sleep(10000);
        }

        private void UpdateServiceConfigFile(VectorTileConfig tileConfig)
        {
            string configFile = Path.Combine(PATH_Config, "TileConfig_service.xml");
            string configServiceFile = Path.Combine(PATH_Config, "TileConfig.xml");
            if (!File.Exists(configFile))
            {
                SerializeUtil.XmlSerialize(tileConfig, configServiceFile);
            }
            else
            {
                VectorTileConfig config = SerializeUtil.XmlDeserialize<VectorTileConfig>(configFile);
                foreach (var item in config.Layers)
                {
                    if (item.TableName == tileConfig.Layers[0].TableName)
                    {
                        config.Layers.Remove(item);
                        break;
                    }
                }

                config.Layers.Add(tileConfig.Layers[0]);

                SerializeUtil.XmlSerialize(config, configServiceFile);
            }
        }

        #region close
        private bool CloseTools()
        {
            CloseTool("java");
            CloseTool("node");

            return true;
        }

        private bool CloseTool(string processName)
        {
            foreach (Process p in System.Diagnostics.Process.GetProcessesByName(processName))
            {
                try
                {
                    p.Kill();
                    p.WaitForExit();
                }
                catch (Exception ex)
                {
                    LogHelper.Error.Append(ex);
                }
            }

            return true;
        }

        #endregion

        private string GetConfigValue(string name, string defaultValue = "")
        {
            string val = System.Configuration.ConfigurationManager.AppSettings.Get(name);
            if (val == null)
            {
                return defaultValue;
            }
            return val;
        }
    
    }    
}
