using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HttpLogger.Monitors
{
	public class ProxyResponse
	{
		private static readonly Regex CookieSplitRegEx = new Regex(@",(?! )", RegexOptions.Compiled);
		private const int BufferSize = 8192;

		public void Process(ProxyRequest request)
		{
			if (request.Method.ToUpper() == "POST")
			{
				var postBuffer = new char[request.ContentLength];
				int bytesRead;
				var totalBytesRead = 0;
				var sw = new StreamWriter(request.HttpRequest.GetRequestStream());
				while (totalBytesRead < request.ContentLength && (bytesRead = request.ClientStreamReader.ReadBlock(postBuffer, 0, request.ContentLength)) > 0)
				{
					totalBytesRead += bytesRead;
					sw.Write(postBuffer, 0, bytesRead);
				}

				sw.Close();
			}

			request.HttpRequest.Timeout = 15000;
			HttpWebResponse response;

			try
			{
				response = (HttpWebResponse)request.HttpRequest.GetResponse();
			}
			catch (WebException webEx)
			{
				response = webEx.Response as HttpWebResponse;
			}
			if (response != null)
			{
				var responseHeaders = ReadResponseHeaders(response);

				var outStream = request.IsHttps ? request.SslStream : request.ClientStream;

				var myResponseWriter = new StreamWriter(outStream);
				var responseStream = response.GetResponseStream();
				try
				{
					//send the response status and response headers
					WriteResponseStatus(response.StatusCode, response.StatusDescription, myResponseWriter);
					WriteResponseHeaders(myResponseWriter, responseHeaders);


					var buffer = response.ContentLength > 0 ? new byte[response.ContentLength] : new byte[BufferSize];

					int bytesRead;

					while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
					{
						outStream.Write(buffer, 0, bytesRead);
					}
					responseStream.Close();
					outStream.Flush();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
				finally
				{
					responseStream.Close();
					response.Close();
					myResponseWriter.Close();
				}
			}
		}

		private static void WriteResponseStatus(HttpStatusCode code, string description, StreamWriter myResponseWriter)
		{
			var s = $"HTTP/1.0 {(int)code} {description}";
			myResponseWriter.WriteLine(s);
		}

		private static void WriteResponseHeaders(StreamWriter myResponseWriter, List<Tuple<string, string>> headers)
		{
			if (headers != null)
			{
				foreach (Tuple<string, string> header in headers)
					myResponseWriter.WriteLine($"{header.Item1}: {header.Item2}");
			}
			myResponseWriter.WriteLine();
			myResponseWriter.Flush();
		}

		private static List<Tuple<string, string>> ReadResponseHeaders(HttpWebResponse response)
		{
			string value = null;
			string header = null;
			var returnHeaders = new List<Tuple<string, string>>();
			foreach (string s in response.Headers.Keys)
			{
				if (s.ToLower() == "set-cookie")
				{
					header = s;
					value = response.Headers[s];
				}
				else
					returnHeaders.Add(new Tuple<string, string>(s, response.Headers[s]));
			}

			if (!string.IsNullOrWhiteSpace(value))
			{
				response.Headers.Remove(header);
				var cookies = CookieSplitRegEx.Split(value);
				returnHeaders.AddRange(cookies.Select(cookie => new Tuple<string, string>("Set-Cookie", cookie)));
			}
			returnHeaders.Add(new Tuple<string, string>("X-Proxied-By", "http-logger.net"));
			return returnHeaders;
		}
	}
}
