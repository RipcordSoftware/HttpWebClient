using System;
using System.IO;

using RipcordSoftware.HttpWebClient;

namespace HttpWebClientExample
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            try
            {
                var request = new HttpWebClientRequest(@"http://www.ietf.org/rfc/rfc2616.txt");

                using (var response = request.GetResponse())
                {
                    if (response.StatusCode == (int)HttpWebClientStatusCode.OK)
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            using (var stream = new StreamReader(responseStream))
                            {
                                var body = stream.ReadToEnd();
                                Console.WriteLine(body);
                            }
                        }
                    }
                }
            }
            catch (HttpWebClientException ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
    }
}
