using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExportFiddlerData;
using System.Configuration;
using Fiddler;

namespace Console
{
    class Program
    {
        static void Main(string[] args)
        {
            IExportGML exporter = new ExportHosts2GML();// new ExportSessions2GML();
            exporter.FilesToRead = ConfigurationManager.AppSettings["readFile"].Split(new char[] { ';' });
            exporter.OutputFiles = ConfigurationManager.AppSettings["outputFiles"];
            exporter.Directed = bool.Parse(ConfigurationManager.AppSettings["directed"]);
            exporter.Init();
            exporter.WriteGML();
        }
    }
}
