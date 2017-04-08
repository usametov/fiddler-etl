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
    public class ExportSessions2GML : FiddlerSessionsReader, IExportGML
    {
        string outputFile;
        public string OutputFiles
        {
            get { return outputFile; }
            set { outputFile = value; }
        }
        
        const string NODE_FORMAT = @"  node [
    id {0}
    label ""{9}"" 
    bHasResponse ""{1}""
    bypassGateway ""{2}""
    clientIP ""{3}""
    clientPort {4}
    HTTPMethod ""{5}""    
    ResponseCode {6}
    MIMEType ""{7}""
    host ""{8}""
    hostname ""{9}""
    isHTTPS ""{10}""
    isTunnel ""{11}""    
    PathAndQuery ""{12}""   
    port {13}
    URL ""{14}""
    ResponseSize {15}    
    Referer ""{16}""
    HasCookie ""{17}""
    ResponseHeaders ""{18}""
  ]";

        const string EDGE_FORMAT = @"  edge [
    id {0}
    source {1}
    target {2}
    value {3}
  ]";        

        public void WriteGML()
        {
            try
            {
                Monitor.Enter(this.AllSessions);

                if (this.AllSessions.Count > 0)
                {
                    var edges = new List<Edge>();
                    int edgeCounter = 1;
                    using (var writer = File.CreateText(this.OutputFiles))
                    {
                        writer.WriteLine("graph [");                        
                        writer.WriteLine(string.Format("  directed {0}", this.Directed?1:0));

                        foreach (Session oS in this.AllSessions)
                        {
                            writer.Write(string.Format(NODE_FORMAT, oS.id, oS.bHasResponse, oS.bypassGateway, oS.clientIP
                                 , oS.clientPort, oS.oRequest.headers.HTTPMethod, oS.responseCode
                                 , oS.oResponse.MIMEType, oS.host, oS.hostname, oS.isHTTPS, oS.isTunnel
                                 , oS.PathAndQuery, oS.port, oS.url, oS.responseBodyBytes.Length, oS.oRequest.headers["referer"], !string.IsNullOrEmpty(oS.oRequest.headers["cookie"]), string.Empty
                                 /* oS.oResponse.headers.ToString().Replace("\r\n", "\t")*/));
                            
                            writer.Write("\r\n");
                            //referer is not empty
                            if (!string.IsNullOrEmpty(oS.oRequest.headers["referer"]))
                            {//sometimes web sites refer to themselves causing loops, this will be taken care separately
                             //many of them could be trackers
                                var referers = this.AllSessions.FindAll(s => oS.oRequest.headers["referer"].ToLower().Contains(s.hostname.ToLower()));
                               
                                if (oS.uriContains("referer"))
                                {
                                    if (oS.PathAndQuery.Contains(URL_EMBEDDED_REFERER_KEY))
                                    {
                                        int startIndex = oS.PathAndQuery.IndexOf(URL_EMBEDDED_REFERER_KEY);
                                        referers.AddRange(this.AllSessions.FindAll(s => oS.PathAndQuery.Substring(startIndex + URL_EMBEDDED_REFERER_KEY.Length).Contains(s.hostname.ToLower())));                                       
                                    }
                                }
                                
                                if(referers.Count>0)
                                {//add edge instance
                                    referers.ForEach(r => { 
                                                            edges.Add(new Edge{ ID = edgeCounter, Source = r.id, Target = oS.id, Value = 1 }); 
                                                                  edgeCounter++;});
                                }
                            }                                                        
                        }
                        //serialize edges
                        foreach (var edge in edges)
                        {
                            writer.Write(string.Format(EDGE_FORMAT, edge.ID, edge.Source, edge.Target,edge.Value));
                            writer.Write("\r\n");
                        }

                        writer.WriteLine("]");
                    }
                }
            }
            finally
            {
                Monitor.Exit(this.AllSessions);
            }
        }

        public bool Directed { get; set; }
    }
}
