using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ServiceHelpers
{
    public static class TranslationServiceHelper
    {
        public static int RetryCountOnQuotaLimitError = 6;
        public static int RetryDelayOnQuotaLimitError = 500;

        public static Action Throttled;

        private static string apiKey;
        public static string ApiKey
        {
            get
            {
                return apiKey;
            }

            set
            {
                var changed = apiKey != value;
                apiKey = value;
            }
        }

        private static async Task<TResponse> RunTaskWithAutoRetryOnQuotaLimitExceededError<TResponse>(Func<Task<TResponse>> action)
        {
            int retriesLeft = TranslationServiceHelper.RetryCountOnQuotaLimitError;
            int delay = TranslationServiceHelper.RetryDelayOnQuotaLimitError;

            TResponse response = default(TResponse);

            while (true)
            {
                try
                {
                    response = await action();
                    break;
                }
                catch (Microsoft.ProjectOxford.Vision.ClientException exception) when (exception.HttpStatus == (System.Net.HttpStatusCode)429 && retriesLeft > 0)
                {
                    ErrorTrackingHelper.TrackException(exception, "Vision API throttling error");
                    if (retriesLeft == 1 && Throttled != null)
                    {
                        Throttled();
                    }

                    await Task.Delay(delay);
                    retriesLeft--;
                    delay *= 2;
                    continue;
                }
            }

            return response;
        }

        private static async Task RunTaskWithAutoRetryOnQuotaLimitExceededError(Func<Task> action)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError<object>(async () => { await action(); return null; });
        }


        public static async Task<string[]> TranslateArray(string[] translateArraySourceTexts)
        {
            var from = "en";
            var to = "ar";
            var uri = "https://api.microsofttranslator.com/v2/Http.svc/TranslateArray";
            var sb = new StringBuilder();

            var body = "<TranslateArrayRequest>" +
                           "<AppId />" +
                           "<From>{0}</From>" +
                           "<Options>" +
                           " <Category xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<ContentType xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\">{1}</ContentType>" +
                               "<ReservedFlags xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<State xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<Uri xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<User xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                           "</Options>" +
                           "<Texts>";
            sb.AppendFormat(body, from, "text/plain");
            foreach (string s in translateArraySourceTexts)
            {
                sb.AppendFormat("<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">{0}</string>", s);
            }


            sb.AppendFormat("</Texts>" +
                           "<To>{0}</To>" +
                       "</TranslateArrayRequest>", to);
            string requestBody = sb.ToString();
            
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "text/xml");
                request.Headers.Add("Ocp-Apim-Subscription-Key", "7ff9bad6b73d425da464201b2e900c2c");
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var doc = XDocument.Parse(responseBody);
                    var ns = XNamespace.Get("http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2");
                    var sourceTextCounter = 0;
                    string[] result = new string[translateArraySourceTexts.Length];
                    foreach (XElement xe in doc.Descendants(ns + "TranslateArrayResponse"))
                    {
                        foreach (var node in xe.Elements(ns + "TranslatedText"))
                        {
                            result[sourceTextCounter] = node.Value;
                        }
                        sourceTextCounter++;
                    }
                    return result;
                }
                else return null;
            }
        }

        public static async Task<string> TranslateString(string translateSourceText)
        {
            var from = "en";
            var to = "ar";
            var uri = "https://api.microsofttranslator.com/v2/Http.svc/TranslateArray";
            var sb = new StringBuilder();

            var body = "<TranslateArrayRequest>" +
                           "<AppId />" +
                           "<From>{0}</From>" +
                           "<Options>" +
                           " <Category xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<ContentType xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\">{1}</ContentType>" +
                               "<ReservedFlags xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<State xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<Uri xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<User xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                           "</Options>" +
                           "<Texts>";
            sb.AppendFormat(body, from, "text/plain");
            sb.AppendFormat("<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">{0}</string>", translateSourceText);
            sb.AppendFormat("</Texts>" +
                           "<To>{0}</To>" +
                       "</TranslateArrayRequest>", to);
            string requestBody = sb.ToString();

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "text/xml");
                request.Headers.Add("Ocp-Apim-Subscription-Key", "7ff9bad6b73d425da464201b2e900c2c");
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var doc = XDocument.Parse(responseBody);
                    var ns = XNamespace.Get("http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2");
                    var sourceTextCounter = 0;
                    var value = "";
                    foreach (XElement xe in doc.Descendants(ns + "TranslateArrayResponse"))
                    {
                        foreach (var node in xe.Elements(ns + "TranslatedText"))
                        {
                            value =  node.Value;
                        }
                        sourceTextCounter++;
                    }
                    return value;
                }
                else return null;
            }
        }

    }
}
