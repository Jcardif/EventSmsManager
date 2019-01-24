using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AfricasTalkingCS;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace SendSms
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            var parameters = req.GetQueryNameValuePairs().ToList();
            var phoneNo = parameters[1].Value;
            var messageCode = parameters[2].Value;
            var firstName = parameters[3].Value;

            var settings = new AppSettings();

            //var phoneNo4 = req.GetQueryNameValuePairs()
            //    .FirstOrDefault(q => string.Compare(q.Key, "phoneNo", StringComparison.OrdinalIgnoreCase) == 0)
            //    .Value;

            if (phoneNo == null)
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                phoneNo = data?.phoneNo;
            }

            string message = "";
            var reader = new StreamReader(settings.CsvFileUrl);
            var lst = new List<string>();
            while (!reader.EndOfStream)
            {
                lst.Add(reader.ReadLine());
            }
            bool isAvailable = false;
            var count = 1;
            string code = "";
            switch (messageCode)
            {
                case "RC":
                    while (!isAvailable)
                    {
                        if (count > lst.Count-1)
                        {
                            log.Info("Verification code are over");
                            //Todo: Send to admins
                            return null;
                        }

                        if (string.IsNullOrEmpty(lst[count].Split(',')[3]))
                        {
                            code = lst[count].Split(',')[0];
                            var arr = lst[count].Split(',').ToList();
                            arr.Insert(3, firstName);
                            string line = "";
                            foreach (var ar in arr)
                            {
                                line = $"{line},{ar}";
                            }

                            line.Remove(line.LastIndexOf(line, StringComparison.Ordinal));
                            isAvailable = true;
                            var lines=File.ReadAllLines(settings.CsvFileUrl);
                            lines[count] = line;
                            File.WriteAllLines(settings.CsvFileUrl,lines);
                        }
                        else
                        {
                            count++;
                        }
                    }

                    message =
                        $"Hello, your request for an azure verification code has been processed. Your verification code is {code}. Head to https://aka.ms/azureforstudents to activate your account. Request assistance from the registration desk.";
                    break;
                case "RG":
                    message = "";
                    break;
                default:
                    break;
            }
            var gateway = new AfricasTalkingGateway(settings.UserName, settings.ApiKey);

            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    log.Info("Message not defined");
                    return null;
                }
                var results = gateway.SendMessage(phoneNo, message);
                
            }
            catch (Exception e)
            { 
                log.Info($"An error occured. The exception is {e.Message}");
                log.Info(e.Message);
            }

            return phoneNo == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a phoneNo on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "Hello " + phoneNo);
        }
    }
}
