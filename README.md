[![Build Status](https://travis-ci.org/RipcordSoftware/HttpWebClient.svg?branch=master)](https://travis-ci.org/RipcordSoftware/HttpWebClient)

# HttpWebClient
The HttpWebClient is a C# HTTP Web Client implementation designed to drop-in replace Mono's HttpWebRequest and HttpWebResponse implementation. The main goals were:
* Approximate the Mono/.NET implementation without the unneeded Windows parts
* Keep things simple
* Be fast

HttpWebClient runs under Mono 3.x and .NET 4.5 and above.

# Building

Build from MonoDevelop or from the command line:
```shell
xbuild HttpWebClient.sln
```
To build in release mode execute xbuild with the additional parameter: /p:configuration=Release.
