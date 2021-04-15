using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace NegalWebApi.Controllers
{
	[ApiController]
	public class DebugController : ControllerBase
	{
		private readonly ILogger logger;
		private readonly HttpClient http;
		private readonly IConfiguration conf;

		public DebugController(IConfiguration conf, HttpClient http, ILogger<DebugController> logger)
		{
			this.conf = conf;
			this.http = http;
			this.logger = logger;
		}

		private const string ContentTypeHeader = "Content-Type";
		private const string JsonContentType = "application/json";

		private async ValueTask Debug(string url, Func<string, string> transform = null)
		{
			var method = String.Empty;
			try
			{
				var dtid = String.Empty;
				var up = url.Split('/');
				if (String.Equals("plasmaApi", up[^2], StringComparison.InvariantCultureIgnoreCase)) dtid = up[^1];
				logger.LogInformation($"Called method: {url}.");
				method = String.Join('-', up);
				if (!String.IsNullOrEmpty(dtid))
				{
					using (var fl1 = System.IO.File.OpenWrite($"Dumps/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Request.json"))
					{
						await Request.Body.CopyToAsync(fl1).ConfigureAwait(false);
					}
					string resp;
					var rname = $"Data/{dtid}.json";
					if (!System.IO.File.Exists(rname)) rname = $"Data/Default.json";
					using (var fd = System.IO.File.OpenRead(rname))
					{
						using var sr = new StreamReader(fd);
						resp = await sr.ReadToEndAsync().ConfigureAwait(false);
					}

					if (transform != null)
						resp = transform(resp);

					using (var fl2 = System.IO.File.OpenWrite($"Dumps/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Response.json"))
					{
						using var sw = new StreamWriter(fl2);
						await sw.WriteAsync(resp).ConfigureAwait(false);
					}

					Response.StatusCode = 200;
					Response.Headers.Add(ContentTypeHeader, JsonContentType);
					await Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(resp).AsMemory()).ConfigureAwait(false);
				}
				else
				{
					using (var fl2 = System.IO.File.OpenWrite($"Dumps/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Response_404.json"))
					{
						
					}
					Response.StatusCode = 404;
				}
			}
			catch (Exception e)
			{
				using var fle = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Exception.log");
				using var sw = new StreamWriter(fle);
				await sw.WriteAsync(e.ToString()).ConfigureAwait(false);
				logger.LogError(e, "Unhandled Exception");
				throw;
			}
		}

		private async ValueTask Proxy(string url)
		{
			var method = String.Empty;
			try
			{
				logger.LogInformation($"Called method: {url}.");
				method = url.Replace('/', '-');
				int rcode;
				string[] rscnts;
				using (var rsms = new MemoryStream())
				{
					using (var rqms = new MemoryStream())
					{
						await Request.BodyReader.CopyToAsync(rqms).ConfigureAwait(false);
						rqms.Seek(0, SeekOrigin.Begin);
						using (var fl1 = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Request.json"))
						{
							await rqms.CopyToAsync(fl1).ConfigureAwait(false);
						}

						rqms.Seek(0, SeekOrigin.Begin);
						using var cnt = new StreamContent(rqms);
						IEnumerable<string> cntt = Request.Headers[ContentTypeHeader];
						cnt.Headers.Add(ContentTypeHeader, cntt);

						var ub = new UriBuilder(conf.GetValue<string>("ProxyTarget"))
						{
							Path = url
						};
						using var presp = await http.PostAsync(ub.Uri, cnt).ConfigureAwait(false);
						rcode = (int)presp.StatusCode;
						rscnts = presp.Headers.Where(h => h.Key == ContentTypeHeader).Select(h => h.Value).FirstOrDefault()?.ToArray();
						await presp.Content.CopyToAsync(rsms).ConfigureAwait(false);
						rsms.Seek(0, SeekOrigin.Begin);
					}

					using (var fl2 = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Response_{rcode}.json"))
					{
						await rsms.CopyToAsync(fl2).ConfigureAwait(false);
					}
					rsms.Seek(0, SeekOrigin.Begin);

					Response.StatusCode = rcode;
					Response.Headers.Add(ContentTypeHeader, rscnts);
					await Response.BodyWriter.WriteAsync(rsms.ToArray().AsMemory()).ConfigureAwait(false);
				}
			}
			catch (Exception e)
			{
				using var fle = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Exception.log");
				using var sw = new StreamWriter(fle);
				await sw.WriteAsync(e.ToString()).ConfigureAwait(false);
				logger.LogError(e, "Unhandled Exception");
				throw;
			}
		}

		private static readonly Regex rg = new(@"\{(\{[^\}]*\})\}", RegexOptions.Compiled);

		[HttpPost]
		[Route("{*url}")]
		public ValueTask Wildcard(string url)
		{
			return conf.GetValue<string>("Mode") switch
			{
				nameof(Debug) => Debug(url, s => rg.Replace(s, new MatchEvaluator(m => String.Format(m.Groups[1].Value, DateTime.Now)))),
				nameof(Proxy) => Proxy(url),
				_ => throw new Exception("Mode is not set"),
			};
		}
	}
}
