using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExportFiddlerData
{
    /// <summary>
    /// trying to measure information passed to third party
    /// </summary>
    public class Edge
    {
        public int ID { get; set; }
        public int Source { get; set; }
        public int Target { get; set; }
        public decimal Value { get; set; }
        public string RefererHostName { get; set; }
        public string TargetHostName { get; set; }
        public string TargetRootDomain { get; set; }
        public string RefererRootDomain { get; set; }

        public string HTTPMethod{ get; set; }
        public int ResponseCode{ get; set; }
        public string ResponseMIMEType{ get; set; }
        public long ResponseLength{ get; set; }
        
        public bool HasCookie { get; set; }
        public bool HasQueryString { get; set; }
        public bool QueryStringHasUrl { get; set; }
        public int CookieBagSize { get; set; }
        public bool CookieHasLogin { get; set; }
        public int PathAndQueryLength { get; set; }
        public bool RefererHasQueryString { get; set; }

        public string RequestHeadersRaw { get; set; }

        public string ResponseHeadersRaw { get; set; }

        public string CookiesRaw { get; set; }

        public bool HasResponseLocationURL{ get; set; }
        public string ResponseLocation { get; set; }
        
    }

    /// <summary>
    /// holds host info
    /// </summary>
    public class HostNode
    {
        int id;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        string hostName;
        /// <summary>
        /// this could be subdomain
        /// </summary>
        public string HostName
        {
            get { return hostName; }
            set { hostName = value; }
        }

        string host;
        /// <summary>
        /// this might be different from hostname, it specifies root domain
        /// </summary>
        public string Host
        {
            get { return host; }
            set { host = value; }
        }       
    }
}
