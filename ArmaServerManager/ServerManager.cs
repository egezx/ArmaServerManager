﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Web.Script.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ArmaServerManager
{
    public static class ServerManager
    {
        private static Settings s = SettingsManager.LoadSettings();

        public static List<SrvProcPair> ServerList = new List<SrvProcPair>();

        #region ServerManagingMethods
        public static string RestartServer(SrvProcPair spp)
        {
            StopServer(spp);
            return StartServer(spp);
        }

        public static void StopServer(SrvProcPair spp)
        {
            try
            {
                spp.proc.Kill();
                spp.proc.WaitForExit();

            }
            catch (Exception)
            {

                Console.WriteLine("Warning: Failed to stop server");
                return;
            }
        }

        public static string StartServer(SrvProcPair spp)
        {
            try
            {           
                Arma3Server server = spp.serverData;
                Arma3ServerConfigWriter.WriteConfigFile(server, s);
                string RelativePath = s.ArmaServersDataPath + '\\' + server.ServerID;
                Process p = Process.Start(s.Arma3ServerExePath, "\"-port=" + server.GamePort + "\" \"-config=" + RelativePath + "\\serverconfig.cfg\"" + " \"-profiles=" + RelativePath + "\" \"-name=" + server.ServerProfileName + "\" \"-bepath=" + s.BattlEyePath+"\"");
                spp.proc = p;
                return "SERVER_START_SUCCEEDED";
            }
            catch (Exception e )
            {
                Console.WriteLine("Error: Failed to start server {0}",e.Message);
                return "SERVER_START_FAILED";
            }

        }

        public static SrvProcPair CreateNewSrvProcInstance(Arma3Server server)
        {
            SrvProcPair spp = new SrvProcPair();
            spp.serverData = server;
            ServerList.Add(spp);
            SaveServerList();
            return spp;
        }

        public static SrvProcPair CreateNewServer()
        {
            Arma3Server s = new Arma3Server();
            s.ServerID = FindFreeID();
            return CreateNewSrvProcInstance(s);
        }

        private static int FindFreeID()
        {
            int id = 0;
            while (true)
            {
                bool taken = false;
                foreach (var item in ServerList)
                {
                    if (item.serverData.ServerID == id) taken = true;
                }
                if (!taken)
                    return id;
                id++;
            }
        }

        #endregion


        #region ServerDataHandlingMethods
        public static string GetServerDataByID(int id)
        {
            StringBuilder sb = new StringBuilder();
            Arma3Server server = FindServerByID(id);
            if (server == null) return string.Empty;
            return new JavaScriptSerializer().Serialize(server);
        }


        public static string UpdateServerParameter(int serverId, string paramName, object value)
        {
            Arma3Server server = FindServerByID(serverId);
            if (server == null)
                return "SERVER_ID_NOT_FOUND";
            SrvParam param = FindSrvParam(server, paramName);
            if (param == null)
                return "PARAMETER_NOT_FOUND";


            param.paramValue = value;
            SaveServerList();

            return "PARAMETER_UPDATED";

        }

        public static string UpdateServerParamState(int serverId, string paramName, bool state)
        {
            Arma3Server server = FindServerByID(serverId);
            if (server == null)
                return "SERVER_ID_NOT_FOUND";
            SrvParam param = FindSrvParam(server, paramName);
            if (param == null)
                return "PARAMETER_NOT_FOUND";


            param.include = state;
            SaveServerList();

            return "PARAMETER_UPDATED";
        }

        public static string HandleRequest(List<string[]> request)
        {
            string requestName = FindRequestValue(request, "request");
            int id;
            switch (requestName)
            {
                
                case "updateserverparam":
                    if(int.TryParse(FindRequestValue(request,"serverid"), out id))
                        return UpdateServerParameter(id, FindRequestValue(request,"param"),FindRequestValue(request,"paramvalue"));
                    return "INVALID_SERVER_ID_DATATYPE";

                case "updateserverparamstate":
                    if (int.TryParse(FindRequestValue(request, "serverid"), out id))
                        return UpdateServerParamState(id, FindRequestValue(request, "param"), Convert.ToBoolean(FindRequestValue(request, "paramvalue")));
                    return "INVALID_SERVER_ID_DATATYPE";

                case "updateport":
                    try
                    {
                        FindServerByID(Convert.ToInt32(FindRequestValue(request, "serverid"))).GamePort = Convert.ToInt32(FindRequestValue(request,"paramvalue"));
                        return "GAMEPORT_UPDATED";
                    }
                    catch (Exception)
                    {
                        return "INVALID_PORT_OR_SERVER_ID";
                    }

                case "updatequeryport":
                    try
                    {
                        FindServerByID(Convert.ToInt32(FindRequestValue(request, "serverid"))).QueryParams.Port = Convert.ToInt32(FindRequestValue(request, "paramvalue"));
                        return "QUERY_PORT_UPDATED";
                    }
                    catch (Exception)
                    {
                        return "INVALID_PORT_OR_SERVER_ID";
                    }

                case "updatequeryip":
                    try
                    {
                        FindServerByID(Convert.ToInt32(FindRequestValue(request, "serverid"))).QueryParams.IPAddress = FindRequestValue(request, "paramvalue");
                        return "QUERY_IP_UPDATED";
                    }
                    catch (Exception)
                    {
                        return "INVALID_IP_OR_SERVER_ID";
                    }

                case "updaterconip":
                    try
                    {
                        FindServerByID(Convert.ToInt32(FindRequestValue(request, "serverid"))).RconParams.IPAddress = FindRequestValue(request, "paramvalue");
                        return "RCON_IP_UPDATED";
                    }
                    catch (Exception)
                    {
                        return "INVALID_IP_OR_SERVER_ID";
                    }

                case "updaterconport":
                    try
                    {
                        FindServerByID(Convert.ToInt32(FindRequestValue(request, "serverid"))).RconParams.Port = Convert.ToInt32(FindRequestValue(request, "paramvalue"));
                        return "RCON_PORT_UPDATED";
                    }
                    catch (Exception)
                    {
                        return "INVALID_PORT_OR_SERVER_ID";
                    }

                case "updaterconpassword":
                    try
                    {
                        FindServerByID(Convert.ToInt32(FindRequestValue(request, "serverid"))).RconParams.Password = FindRequestValue(request, "paramvalue");
                        return "RCON_PASSWORD_UPDATED";
                    }
                    catch (Exception)
                    {
                        return "INVALID_SERVERID";
                    }

                case "updateprofilename":
                    try
                    {
                        FindServerByID(Convert.ToInt32(FindRequestValue(request, "serverid"))).ServerProfileName = FindRequestValue(request, "paramvalue");
                        return "PROFILENAME_UPDATED";
                    }
                    catch (Exception)
                    {
                        return "INVALID_SERVER_ID";
                    }

                case "startserver":
                    if (int.TryParse(FindRequestValue(request, "serverid"), out id))
                        return RestartServer(ServerManager.FindServerProcPairByID(id));
                    return "INVALID_SERVER_ID_DATATYPE";

                case "stopserver":
                    if (int.TryParse(FindRequestValue(request, "serverid"), out id))
                        StopServer(ServerManager.FindServerProcPairByID(id));
                    return "SERVER_STOPPED";

                case "serverinfo":
                    if (int.TryParse(FindRequestValue(request, "serverid"), out id))
                    {
                        string serverdata = ServerManager.GetServerDataByID(id);
                        if (serverdata.Length > 0) return serverdata;
                        return "SERVER_NOT_FOUND_WITH_ID_" + id;
                    }
                    return "INVALID_SERVER_ID_DATATYPE";

                case "queryinfo":
                    try
                    {
                        return new JavaScriptSerializer().Serialize(ServerManager.GetQueryInfo(FindServerByID(Convert.ToInt32(FindRequestValue(request, "serverid")))));
                    }
                    catch (Exception)
                    {
                        return "FAILED_TO_RECEIVE_QUERY_INFO";
                    }

                case "deletemission":
                    if (int.TryParse(FindRequestValue(request, "serverid"), out id))
                    {
                        ServerManager.FindServerByID(id).Missions.RemoveSubclassesByName(FindRequestValue(request, "param"));
                        return "REMOVED_MISSION";
                    }

                    return "INVALID_SERVER_ID_DATATYPE";

                case "getmissionfiles":
                    return new JavaScriptSerializer().Serialize(ServerManager.GetMissionFiles());

                case "addnewserver":
                    ServerManager.CreateNewServer();
                    return "NEW_SERVER_CREATED";

                case "addmissiontocycle":
                    if (int.TryParse(FindRequestValue(request, "serverid"), out id))
                    {
                        string name = FindRequestValue(request, "missionname");
                        string file = FindRequestValue(request, "missionfile");
                        string difficulty = FindRequestValue(request,"difficulty");
                        if (name.Length > 0 && file.Length > 0 && difficulty.Length > 0)
                        {
                            ServerManager.FindServerByID(id).Missions.SubClasses.Add(new Arma3MissionClass(name, file, difficulty));
                            return "ADDED_MISSION";
                        }
                        else
                        {
                            return "INVALID_MISSION_PARAMS";
                        }
                    }
                    return "INVALID_SERVER_ID_DATATYPE";


                case "serverlist":
                    return new JavaScriptSerializer().Serialize(ServerList.Select(x => new{x.serverData.ServerID, x.serverData.HostName }).ToArray());

                case "sendrconcommand":
                    try
                    {
                        return SendRconCommand(FindRequestValue(request,"paramvalue"), FindServerByID(Convert.ToInt32(FindRequestValue(request, "serverid"))));
                        
                    }
                    catch (Exception)
                    {
                        return "INVALID_SERVER_ID";
                    }

                default:
                    return "INVALID_REQUEST_TYPE";
            }

        }

        public static Arma3Server FindServerByID(int id)
        {
            Arma3Server server = null;
            foreach (var item in ServerList)
            {
                if (item.serverData.ServerID == id)
                {
                    server = item.serverData;
                    break;
                }
            }

            return server;
        }
        public static SrvProcPair FindServerProcPairByID(int id)
        {
            SrvProcPair spp = null;
            foreach (var item in ServerList)
            {
                if (item.serverData.ServerID == id)
                {
                    spp = item;
                    break;
                }
            }

            return spp;
        }
        private static SrvParam FindSrvParam(Arma3Server s, string paramName)
        {
            foreach (var item in typeof(Arma3Server).GetFields())
            {
                if (item.Name == paramName && item.GetValue(s) is SrvParam)
                    return (SrvParam)item.GetValue(s);
            }

            return null;
        }


        private static string FindRequestValue(List<string[]> requestArray, string requestName)
        {
            foreach (var item in requestArray)
            {
                if (item[0] == requestName)
                    return item[1];
            }
            return "";
        }

        private static string GetProcessPerformance(SrvProcPair spp)
        {
            string data = "PROCESS_NOT_AVAILABLE";

            if (spp.proc != null)
            {
                if (!spp.proc.HasExited)
                {
                    var processData = new
                    {
                        spp.proc.WorkingSet64,
                        spp.proc.TotalProcessorTime
                    };

                    data = new JavaScriptSerializer().Serialize(processData);
                }
            }

            return data;

        }

        private static Dictionary<string,string> GetQueryInfo(Arma3Server server)
        {
            if (server != null)
            {
                SourceQuery query = new SourceQuery();
                return query.GetServerInfo(server.QueryParams.IPAddress, server.QueryParams.Port);
            }

            return new Dictionary<string, string>();
        }

        private static string[] GetMissionFiles()
        {
            List<string> missionList = new List<string>();
            foreach (string file in Directory.GetFiles(s.MissionPath))
            {
                string fName = Path.GetFileName(file);
                if (Path.GetExtension(fName).Equals(".pbo", StringComparison.OrdinalIgnoreCase))
                {
                    missionList.Add(Path.GetFileNameWithoutExtension(fName));
                    Console.WriteLine(fName);
                }
            }

            return missionList.ToArray();
        }

        private static string SendRconCommand(string command, Arma3Server server)
        {
            Rcon.BERcon rcon = new Rcon.BERcon();
            return rcon.SendCommand(command, server.RconParams.IPAddress, server.RconParams.Port, server.RconParams.Password);
        }


        #endregion

        #region ServerSaving

        public static void SaveServerList()
        {
            List<ServerListStorageObject> list = new List<ServerListStorageObject>();

            foreach (var item in ServerList)
            {
                list.Add(new ServerListStorageObject(item.proc, item.serverData, item.serverData.Schedules.ServerEvents));
            }

            if (File.Exists("serversave")) File.Delete("serversave");

            FileStream stream = File.Create("serversave");
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, list);
            stream.Close();
        }

        public static void LoadServerList()
        {
            if (File.Exists("serversave"))
            {
                FileStream stream = File.OpenRead("serversave");
                var formatter = new BinaryFormatter();
                List<ServerListStorageObject> list = formatter.Deserialize(stream) as List<ServerListStorageObject>;
                stream.Close();

                foreach (var item in list)
                {
                    Process p = null;
                    try 
	                {
                        p = Process.GetProcessById(item.ProcessID);
	                }
	                catch (Exception){} 
                    
                    if (p != null)
                    {
                        if (p.ProcessName != item.ProcessName)
                            p = null;
                    }
                    SrvProcPair spp = new SrvProcPair();
                    spp.serverData = item.ServerData;
                    spp.proc = p;
                    spp.serverData.Schedules = new ServerSchedule();
                    spp.serverData.Schedules.ServerEvents = item.Events;
                    ServerList.Add(spp);
                }
            }
        }

        #endregion
    }
}
