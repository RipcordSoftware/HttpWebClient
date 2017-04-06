[![Build Status](https://travis-ci.org/RipcordSoftware/HttpWebClient.svg?branch=master)](https://travis-ci.org/RipcordSoftware/HttpWebClient)
[![Build status](https://ci.appveyor.com/api/projects/status/3cjlb49rdxoxg68i?svg=true)](https://ci.appveyor.com/project/craigminihan/httpwebclient)

# HttpWebClient
The HttpWebClient is a C# HTTP Web Client implementation designed to drop-in replace Mono's HttpWebRequest and HttpWebResponse implementation. The main goals were:
* Approximate the Mono/.NET implementation without the unneeded Windows parts
* Keep things simple
* Be fast

HttpWebClient runs under Mono 3.x and .NET 4.5 and above.

Building
--------
Build from MonoDevelop or from the command line:
```shell
xbuild HttpWebClient.sln
```
To build in release mode execute xbuild with the additional parameter: /p:configuration=Release.

Example
-------
The following example code requests the RFC2616 text file from the IETF web site. The example is very similar to .NETs HttpWebRequest/HttpWebResponse implementation style.
```c#
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
```

Differences and Missing Parts
-------------
* Exceptions are derived from `HttpWebClientException`/ApplicationException and not from the `System.Net` exception types
* There is currently no support for SSL
* `HttpWebRequest.GetResponse()` takes an optional bool parameter which disables exception throws on HTTP 'error' codes. So on 404 or 500 you can get the response and handle it without needing exception plumbing.
