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
    public class FiddlerSessionsReader
    {
        string[] filesToRead;
        public string[] FilesToRead
        {
            get { return filesToRead; }
            set { filesToRead = value; }
        }

        protected const string URL_EMBEDDED_REFERER_KEY = "=http%3A%2F%2F";

        List<Fiddler.Session> oAllSessions;
        protected List<Fiddler.Session> AllSessions
        {
            get { return oAllSessions; }
            set { oAllSessions = value; }
        }

        /// <summary>
        /// set up Fiddler Transcoders and load sessions
        /// </summary>
        public void Init()
        {
            oAllSessions = new List<Fiddler.Session>();
            //string sSAZInfo;
            if (!FiddlerApplication.oTranscoders.ImportTranscoders(Assembly.GetExecutingAssembly()))
            {
                Console.WriteLine("This assembly was not compiled with a SAZ-exporter");
            }
            //else
            //{
            //    sSAZInfo = SAZFormat.GetZipLibraryInfo();
            //}

            ReadSessions(oAllSessions);
        }

        /// <summary>
        /// reads SAZ file which is Fiddler format for exporting captured traffic 
        /// </summary>
        /// <param name="oAllSessions"></param>
        public void ReadSessions(List<Fiddler.Session> oAllSessions)
        {
            TranscoderTuple oImporter = FiddlerApplication.oTranscoders.GetImporter("SAZ");
            if (null != oImporter)
            {
                foreach (var fileToRead in this.FilesToRead)
                {
                    Dictionary<string, object> dictOptions = new Dictionary<string, object>();
                    dictOptions.Add("Filename", fileToRead);

                    Session[] oLoaded = FiddlerApplication.DoImport("SAZ", false, dictOptions, null);

                    if ((oLoaded != null) && (oLoaded.Length > 0))
                    {
                        oAllSessions.AddRange(oLoaded);
                    }
                }
            }
        }
    }
}
