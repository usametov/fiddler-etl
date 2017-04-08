using System;
namespace ExportFiddlerData
{
    public interface IExportGML
    {
        bool Directed { get; set; }
        string[] FilesToRead { get; set; }
        void Init();
        string OutputFiles { get; set; }
        void ReadSessions(System.Collections.Generic.List<Fiddler.Session> oAllSessions);
        void WriteGML();
    }
}
