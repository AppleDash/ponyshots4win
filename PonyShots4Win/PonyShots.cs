using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PonyShots4Win
{
    public class PonyShots
    {
        public string UploadUrl { get; set; }
        public string ImageBaseUrl { get; set; }
        public string Username { get; set; }
        public string ApiKey { get; set; }

        private class JPSResponse
        {
            public bool error;
            public string slug;
            public string message;
        }

        public PonyShotsResponse UploadScreenshot(string shotPath)
        {
            RestClient restClient = new RestClient(UploadUrl);
            RestRequest restRequest = new RestRequest(Method.POST);
            restRequest.AddParameter("username", Username);
            restRequest.AddParameter("apikey", ApiKey);
            restRequest.AddFile("image", shotPath, "image/png");
            var resp = restClient.Execute(restRequest);

            JPSResponse jValue = JsonConvert.DeserializeObject<JPSResponse>(resp.Content);
            PonyShotsResponse psResp = new PonyShotsResponse();

            psResp.RawResponse = resp.Content;
            psResp.Error = jValue.error;

            if (!psResp.Error)
            {   
                psResp.Slug = jValue.slug;
            }
            else
            {
                psResp.ErrorMessage = jValue.message;
            }

            return psResp;
        }

        private string HttpUploadFile(string url, string file, string paramName, string contentType, NameValueCollection nvc)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

            Stream rs = wr.GetRequestStream();

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, paramName, file, contentType);
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);

            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                rs.Write(buffer, 0, bytesRead);
            }
            fileStream.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            WebResponse wresp = null;
            try
            {
                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                return reader2.ReadToEnd();
            }
            catch (Exception)
            {
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
            }
            finally
            {
                wr = null;
            }

            return "";
        }
    }

    public class PonyShotsResponse
    {
        public string RawResponse { get; set; }
        public bool Error { get; set; }
        public string Slug { get; set; }
        public string ErrorMessage { get; set; }
    }
}
