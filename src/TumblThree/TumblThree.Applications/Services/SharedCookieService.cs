using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace TumblThree.Applications.Services
{
    [Export(typeof(ISharedCookieService)), Export]
    public class SharedCookieService : ISharedCookieService
    {
        private readonly CookieContainer cookieContainer = new CookieContainer(); // used to store dot prefixed cookies that cannot be stored to disk because "http://.domain.com" is not a vailid Uri

        public void GetUriCookie(CookieContainer request, Uri uri)
        {
            foreach (Cookie cookie in cookieContainer.GetCookies(uri))
            {
                request.Add(cookie);
            }
        }

        public void SetUriCookie(CookieCollection cookies)
        {
            foreach (Cookie cookie in cookies)
            {
                cookieContainer.Add(cookie);
            }
        }

        public void RemoveUriCookie(Uri uri)
        {
            var cookies = cookieContainer.GetCookies(uri);
            foreach (Cookie cookie in cookies)
            {
                cookie.Expired = true;
            }
        }

        public void Serialize(string path)
        {
            List<Cookie> cookieList = new List<Cookie>(GetAllCookies(cookieContainer));
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                    stream, Encoding.UTF8, true, true, "  "))
                {
                    var serializer = new DataContractJsonSerializer(typeof(List<Cookie>));
                    serializer.WriteObject(writer, cookieList);
                    writer.Flush();
                }
            }
        }

        public void Deserialize(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(List<Cookie>));
                    List<Cookie> cookies = (List<Cookie>)serializer.ReadObject(stream);
                    foreach (Cookie cookie in cookies)
                    {
                        cookieContainer.Add(cookie);
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }
        }

        public static IEnumerable<Cookie> GetAllCookies(CookieContainer c)
        {
            Hashtable k = (Hashtable)c.GetType().GetField("m_domainTable", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(c);
            foreach (DictionaryEntry element in k)
            {
                SortedList l = (SortedList)element.Value.GetType().GetField("m_list", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(element.Value);
                foreach (var e in l)
                {
                    var cl = (CookieCollection)((DictionaryEntry)e).Value;
                    foreach (Cookie fc in cl)
                    {
                        yield return fc;
                    }
                }
            }
        }
    }
}
