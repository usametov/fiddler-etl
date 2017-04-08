using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fiddler;
using System.Threading;
using System.IO;
using Ionic.Zip;
using System.Reflection;

namespace ExportFiddlerData
{
    public class ExportHosts2GML : FiddlerSessionsReader, IExportGML
    {
        public bool Directed
        {
            get;
            set;
        }
               
        string outputFiles;
        public string OutputFiles
        {
            get { return outputFiles; }
            set { outputFiles = value; }
        }

        int hostIDSeed = 1;

        public int HostIDSeed
        {
            get { return hostIDSeed; }
            set { hostIDSeed = value; }
        }

        int edgeIDSeed = 1;

        public int EdgeIDSeed
        {
            get { return edgeIDSeed; }
            set { edgeIDSeed = value; }
        }

        /// <summary>
        /// serializes graph to output file
        /// </summary>
        public void WriteGML()
        {           
            if (this.AllSessions.Count > 0)
            {
                this.HostsDictionary = new Dictionary<string, HostNode>();                
                this.Edges = new List<Edge>();       

                foreach (Session oS in this.AllSessions)
                {
                    if (string.IsNullOrEmpty(oS.hostname))
                        continue;

                    if(!HostsDictionary.ContainsKey(oS.hostname.ToLower().Trim()))
                    {
                        AddNewHost(oS.hostname, oS.host.GetRootDomain());
                        string referer = string.IsNullOrEmpty(oS.oRequest.headers["referer"].ToLower().Trim()) ? string.Empty : new System.Uri(oS.oRequest.headers["referer"].ToLower().Trim()).Host;
                        
                        if (!string.IsNullOrEmpty(referer) 
                         && !referer.Contains(oS.hostname.Replace("www.","").ToLower().Trim()))
                        {
                            AddEdges(oS, referer);
                        }
                    }
                    else if (!string.IsNullOrEmpty(oS.oRequest.headers["referer"])
                          && !this.Edges.Any(e => e.RefererHostName.Equals(new System.Uri(oS.oRequest.headers["referer"].ToLower().Trim()).Host) && e.TargetHostName.Equals(oS.hostname.ToLower().Trim()))
                          && !new System.Uri(oS.oRequest.headers["referer"].ToLower().Trim()).Host.Contains(oS.hostname.Replace("www.", "").ToLower().Trim()))
                    {
                        AddEdges(oS, new System.Uri(oS.oRequest.headers["referer"].ToLower().Trim()).Host);
                    }
                }

                Utils.Serialize(HostsDictionary, this.Edges, this.OutputFiles, this.Directed);
            }           
        }

        private string GetHostNameFromQueryString(Session oS)
        {
            string result = string.Empty;
            if (oS.PathAndQuery.Contains(URL_EMBEDDED_REFERER_KEY))
            {
                int startIndex = oS.PathAndQuery.IndexOf(URL_EMBEDDED_REFERER_KEY);
                var refSession = this.AllSessions.Find(s => oS.PathAndQuery.Substring(startIndex + URL_EMBEDDED_REFERER_KEY.Length).Contains(s.hostname.ToLower()));
                result = refSession == null ? string.Empty : refSession.hostname.ToLower().Trim();
            }

            return result;
        }

        private void AddNewHost(string hostname, string rootDomain)
        {            
            var host = new HostNode { Id = this.HostIDSeed, Host = rootDomain, HostName = hostname};
            HostsDictionary.Add(hostname.ToLower().Trim(), host);
            this.HostIDSeed++;            
        }
        
        private void AddEdges(Session oS, string referer)
        {
            if (!this.HostsDictionary.ContainsKey(referer))
                return;

            var edge = this.Edges.Find(e => e.RefererHostName.Equals(referer) && e.TargetHostName.Equals(oS.hostname.ToLower().Trim()));
            if (edge == null)
            {
                var refererHost = this.HostsDictionary[referer];
                var host = this.HostsDictionary[oS.hostname];
                string refererUrl = oS.oRequest.headers["referer"];
                string responseLocation = string.Empty;

                if (oS.oResponse.headers.Exists("Location") && oS.oResponse.headers["Location"].IsUrl())
                    responseLocation = oS.oResponse.headers["Location"].Split(new char[]{'?'})[0].GetRootDomain();

                this.Edges.Add(new Edge
                                {
                                    ID = this.EdgeIDSeed
                                    ,
                                    Source = refererHost.Id
                                    ,
                                    Target = host.Id
                                    ,
                                    RefererHostName = referer
                                    ,
                                    TargetHostName = oS.hostname                                     
                                    , 
                                    RefererRootDomain = referer.GetRootDomain()
                                    ,
                                    TargetRootDomain = oS.GetRootDomain()
                                    ,
                                    HasCookie = !string.IsNullOrEmpty(oS.oRequest.headers["cookie"])
                                    ,
                                    HTTPMethod = oS.oRequest.headers.HTTPMethod
                                    ,
                                    ResponseCode = oS.responseCode
                                    ,
                                    ResponseMIMEType = oS.oResponse.MIMEType
                                    ,
                                    ResponseLength = oS.responseBodyBytes.Length
                                    ,
                                    Value = 1
                                    ,
                                    CookieBagSize = string.IsNullOrEmpty(oS.oRequest.headers["cookie"]) ? 0 : oS.oRequest.headers["cookie"].Split(new char[] { ';', ':' }).Length
                                    ,
                                    HasQueryString = oS.PathAndQuery.Split(new char[] { '?'}).Length > 1
                                    ,
                                    /*CookieHasLogin = oS.oRequest.headers["cookie"].ToLower().Contains("login")
                                    ,*/ 
                                    PathAndQueryLength = oS.PathAndQuery.Length
                                    , 
                                    QueryStringHasUrl = oS.fullUrl.HasUrl()
                                    ,
                                    RefererHasQueryString = refererUrl.Split(new char[] { '?' }).Length > 1
                                    ,
                                    CookiesRaw = oS.oRequest.headers["cookie"]
                                    ,
                                    RequestHeadersRaw = oS.oRequest.headers.ToString()
                                    ,
                                    ResponseHeadersRaw = oS.oResponse.headers.ToString()
                                    , 
                                    HasResponseLocationURL = !string.IsNullOrEmpty(responseLocation) 
                                    , 
                                    ResponseLocation = responseLocation
                                });
                
                this.EdgeIDSeed++;

                if (!string.IsNullOrEmpty(responseLocation))
                {
                    var forward2Host = oS.oResponse.headers["Location"].Split(new char[] { '?' })[0].GetHostName();
                    if(!this.HostsDictionary.ContainsKey(forward2Host))
                    {
                        AddNewHost(forward2Host, forward2Host.GetRootDomain());                        
                    }

                    AddEdge(oS.oResponse.headers["Location"].Split(new char[] { '?' })[0].GetHostName(), oS.hostname);                    
                }

                //some trackers pass url in query string                            
                string hostNameInQueryString = GetHostNameFromQueryString(oS);
                if (!string.IsNullOrEmpty(hostNameInQueryString))
                {
                    AddEdge(hostNameInQueryString, referer);
                }
            }
        }

        private void AddEdge(string targetHostName, string referer)
        {
            if (!this.HostsDictionary.ContainsKey(referer))
                return;

            if (referer.Contains(targetHostName))
                return;

            if (!this.HostsDictionary.ContainsKey(targetHostName))
                return;

            var edge = this.Edges.Find(e => e.RefererHostName.Equals(referer) && e.TargetHostName.Equals(targetHostName));
            if (edge == null)
            {
                var refererHost = this.HostsDictionary[referer];
                var host = this.HostsDictionary[targetHostName];
                this.Edges.Add(new Edge
                {
                    ID = edgeIDSeed
                    ,
                    Source = refererHost.Id
                    ,
                    Target = host.Id
                    ,
                    RefererHostName = referer
                    , 
                    RefererRootDomain = referer.GetRootDomain()
                    ,
                    TargetHostName = targetHostName
                    , 
                    TargetRootDomain = targetHostName.GetRootDomain()
                    ,
                    HasCookie = false
                    ,
                    HTTPMethod = "FORWARD"
                    ,
                    ResponseCode = -1
                    ,
                    ResponseMIMEType = "FORWARD"
                    ,
                    ResponseLength = -1
                    ,
                    Value = 2
                });

                this.EdgeIDSeed++;
            }
        }

        //private void Serialize(Dictionary<string, HostNode> hostsDictionary, List<Edge> edges)
        //{
        //    string gmlFilePath = this.OutputFiles.Split(';')[0];
        //    string additionalInfoFilePath = string.Empty;
        //    if (this.OutputFiles.Split(';').Length > 1)
        //        additionalInfoFilePath = this.OutputFiles.Split(';')[1];

        //    using (var writer = File.CreateText(gmlFilePath))
        //    {
        //        writer.WriteLine("graph [");
        //        writer.WriteLine(string.Format("  directed {0}", this.Directed ? 1 : 0));

        //        foreach (var hostName in hostsDictionary.Keys)
        //        {
        //            var host = hostsDictionary[hostName];
        //            writer.Write(string.Format(NODE_FORMAT, host.Id, hostName, host.Host));
        //            writer.Write("\r\n");
        //        }
        //        //serialize edges
        //        foreach (var edge in edges)
        //        {
        //            writer.Write(string.Format(EDGE_FORMAT, edge.ID, edge.Source, edge.Target, edge.Value, edge.HasCookie ? "1" : "0", edge.HasQueryString ? "1" : "0", edge.QueryStringHasUrl ? "1" : "0", edge.CookieBagSize, edge.PathAndQueryLength, edge.RefererHasQueryString ? "1" : "0", edge.ResponseLength, edge.TargetRootDomain, edge.HTTPMethod, edge.ResponseCode.ToString(), edge.ResponseMIMEType));
        //            writer.Write("\r\n");
        //        }

        //        writer.WriteLine("]");
        //    }

        //    if (string.IsNullOrEmpty(additionalInfoFilePath))
        //        return;

        //    using (var writer = File.CreateText(additionalInfoFilePath))
        //    {
        //        writer.WriteLine("ID,HasCookie,ResponseLength,HasQueryString,QueryStringHasUrl,CookieBagSize,CookieHasLogin,PathAndQueryLength,RefererHasQueryString,RefererHostName,TargetHostName,HTTPMethod,ResponseCode,ResponseMIMEType,RequestHeadersRaw,ResponseHeadersRaw,CookiesRaw");
        //        foreach (var edge in edges)
        //        {
        //            var rowData1 = new string[] { edge.ID.ToString(), edge.HasCookie ? "1" : "0", edge.ResponseLength.ToString(), edge.HasQueryString ? "1" : "0", edge.QueryStringHasUrl ? "1" : "0", edge.CookieBagSize.ToString(), edge.CookieHasLogin ? "1" : "0", edge.PathAndQueryLength.ToString(), edge.RefererHasQueryString ? "1" : "0" };
        //            var rowData2 = new string[] { edge.RefererHostName, edge.TargetHostName, edge.HTTPMethod, edge.ResponseCode.ToString(), edge.ResponseMIMEType, edge.RequestHeadersRaw, edge.ResponseHeadersRaw, edge.CookiesRaw };
        //            writer.WriteLine(string.Format("{0},\"{1}\"", string.Join(",", rowData1), string.Join("\",\"", rowData2)));
        //        }
        //    }
        //}
                
        public List<Edge> Edges { get; set; }

        public Dictionary<string, HostNode> HostsDictionary { get; set; }
    }
}
