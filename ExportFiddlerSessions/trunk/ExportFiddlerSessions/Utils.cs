using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using Fiddler;
using System.IO;

namespace ExportFiddlerData
{
    public static class Utils
    {
        const string URL_PATTERN = @"^(http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*$";
        const string NODE_FORMAT = @"  node [
    id {0}
    label ""{1}"" 
    host ""{2}""
    name ""{1}""     
  ]";

        const string EDGE_FORMAT = @"  edge [
    id {0}
    source {1}
    target {2}
    value {3}
    HasCookie {4}
    HasQueryString {5}
    QueryStringHasUrl {6}
    CookieBagSize {7}    
    PathAndQueryLength {8}
    RefererHasQueryString {9}
    ResponseLength {10}
    HasURLinResponseLocation {11}
    TargetRootDomain ""{12}""
    RefererRootDomain ""{13}""
    HTTPMethod ""{14}""
    ResponseCode ""{15}""
    ResponseMIMEType ""{16}""
    ResponseLocation ""{17}""
  ]";          

        public static bool HasUrl(this string fullUrl)
        {
            bool result = false;
            var queryStringSplit = fullUrl.Split(new char[] { '?' });
            if (queryStringSplit.Length > 1)
            {
                string queryString = queryStringSplit[1];
                var paramsArray = queryString.Split(new char[] { '&' });
                foreach (var param in paramsArray)
                {
                    var valSplit = param.Split(new char[] { '=' });
                    if (valSplit.Length > 1)
                    {
                        var regex = new Regex(URL_PATTERN);
                        result = regex.IsMatch(HttpUtility.UrlDecode(valSplit[1]));
                    }
                }
            }
            return result;
        }

        public static bool IsUrl(this string fullUrl)
        {
            var queryStringSplit = fullUrl.Split(new char[] { '?' });
            var regex = new Regex(URL_PATTERN);
            return regex.IsMatch(HttpUtility.UrlDecode(queryStringSplit[0]));
        }

        public static string GetRootDomain(this Session oS)
        {
            var domainParts = oS.host.Split(new char[] { '.' });
            int domainPartsCount = domainParts.Length;
            string rootDomain = oS.host;
            if (domainPartsCount > 2)
                rootDomain = string.Format("{0}.{1}", domainParts[domainPartsCount - 2], domainParts[domainPartsCount - 1]);
            return rootDomain;
        }

        public static string GetRootDomain(this string hostName)
        {
            if (hostName.StartsWith("http://"))
                hostName = hostName.Substring("http://".Length);

            if (hostName.StartsWith("https://"))
                hostName = hostName.Substring("https://".Length);

            hostName = hostName.Split(new char[] { '/' })[0];

            var domainParts = hostName.Split(new char[] { '.' });
            int domainPartsCount = domainParts.Length;
            string rootDomain = hostName;
            if (domainPartsCount > 2)
                rootDomain = string.Format("{0}.{1}", domainParts[domainPartsCount - 2], domainParts[domainPartsCount - 1]);
            
            return rootDomain;
        }

        public static string GetHostName(this string url)
        {
            if (url.StartsWith("http://"))
                url = url.Substring("http://".Length);

            if (url.StartsWith("https://"))
                url = url.Substring("https://".Length);

            string hostName = url.Split(new char[] { '/' })[0];

            //var domainParts = url.Split(new char[] { '.' });
            //int domainPartsCount = domainParts.Length;
            
            //if (domainPartsCount > 2)
            //    rootDomain = string.Format("{0}.{1}", domainParts[domainPartsCount - 2], domainParts[domainPartsCount - 1]);

            return hostName;
        }

        public static void Serialize(Dictionary<string, HostNode> hostsDictionary, List<Edge> edges, string outputFiles, bool directed)
        {
            string gmlFilePath = outputFiles.Split(';')[0];
            string additionalInfoFilePath = string.Empty;
            if (outputFiles.Split(';').Length > 1)
                additionalInfoFilePath = outputFiles.Split(';')[1];

            using (var writer = File.CreateText(gmlFilePath))
            {
                writer.WriteLine("graph [");
                writer.WriteLine(string.Format("  directed {0}", directed ? 1 : 0));

                foreach (var hostName in hostsDictionary.Keys)
                {
                    var host = hostsDictionary[hostName];
                    writer.Write(string.Format(NODE_FORMAT, host.Id, hostName, host.Host));
                    writer.Write("\r\n");
                }
                //serialize edges
                foreach (var edge in edges)
                {
                    writer.Write(string.Format(EDGE_FORMAT, edge.ID, edge.Source, edge.Target, edge.Value, edge.HasCookie ? "1" : "0", edge.HasQueryString ? "1" : "0", edge.QueryStringHasUrl ? "1" : "0", edge.CookieBagSize, edge.PathAndQueryLength, edge.RefererHasQueryString ? "1" : "0", edge.ResponseLength, edge.HasResponseLocationURL ? "1" : "0", edge.TargetRootDomain, edge.RefererRootDomain, edge.HTTPMethod, edge.ResponseCode.ToString(), edge.ResponseMIMEType, edge.ResponseLocation));
                    writer.Write("\r\n");
                }

                writer.WriteLine("]");
            }

            if (string.IsNullOrEmpty(additionalInfoFilePath))
                return;

            using (var writer = File.CreateText(additionalInfoFilePath))
            {
                writer.WriteLine("ID,HasCookie,ResponseLength,HasQueryString,QueryStringHasUrl,CookieBagSize,CookieHasLogin,PathAndQueryLength,RefererHasQueryString,RefererHostName,TargetHostName,HTTPMethod,ResponseCode,ResponseMIMEType,RequestHeadersRaw,ResponseHeadersRaw,CookiesRaw");
                foreach (var edge in edges)
                {
                    var rowData1 = new string[] { edge.ID.ToString(), edge.HasCookie ? "1" : "0", edge.ResponseLength.ToString(), edge.HasQueryString ? "1" : "0", edge.QueryStringHasUrl ? "1" : "0", edge.CookieBagSize.ToString(), edge.CookieHasLogin ? "1" : "0", edge.PathAndQueryLength.ToString(), edge.RefererHasQueryString ? "1" : "0" };
                    var rowData2 = new string[] { edge.RefererHostName, edge.TargetHostName, edge.HTTPMethod, edge.ResponseCode.ToString(), edge.ResponseMIMEType, edge.RequestHeadersRaw, edge.ResponseHeadersRaw, edge.CookiesRaw };
                    writer.WriteLine(string.Format("{0},\"{1}\"", string.Join(",", rowData1), string.Join("\",\"", rowData2)));
                }
            }
        }
    }
}
