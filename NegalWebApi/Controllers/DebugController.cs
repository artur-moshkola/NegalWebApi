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
using System.Net.Http.Headers;

namespace NegalWebApi.Controllers
{
	[ApiController]
	public class DebugController : ControllerBase
	{
		private readonly ILogger logger;
		private readonly HttpClient http;

		public DebugController(HttpClient http, ILogger<DebugController> logger)
		{
			this.http = http;
			this.logger = logger;
		}

		private async ValueTask<IActionResult> Debug(string url, Func<string, string> transform = null)
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
						await Request.BodyReader.CopyToAsync(fl1);
					}
					string resp;
					var rname = $"Data/{dtid}.json";
					if (!System.IO.File.Exists(rname)) rname = $"Data/Default.json";
					using (var fd = System.IO.File.OpenRead(rname))
					{
						using var sr = new StreamReader(fd);
						resp = await sr.ReadToEndAsync();
					}

					if (transform != null)
						resp = transform(resp);

					using (var fl2 = System.IO.File.OpenWrite($"Dumps/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Response.json"))
					{
						using var sw = new StreamWriter(fl2);
						await sw.WriteAsync(resp);
					}
					return Content(resp, "application/json");
				}

				using var msrq = new MemoryStream();
				await Request.BodyReader.CopyToAsync(msrq);
				using (var fl1 = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Request.json"))
				{
					await msrq.CopyToAsync(fl1);
				}
				
				using var cnt = new StreamContent(msrq);
				cnt.Headers.ContentType = new MediaTypeHeaderValue("application/json");
				using var presp = await http.PostAsync("http://localhost:9191/" + url, cnt);
				if (presp.IsSuccessStatusCode)
				{
					string rsp;
					using (var msrs = new MemoryStream()) {
						await presp.Content.CopyToAsync(msrs);
						rsp = Encoding.UTF8.GetString(msrs.GetBuffer());
					}

					using (var fl2 = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Response.json"))
					{
						using var sw = new StreamWriter(fl2);
						await sw.WriteAsync(rsp);
					}
					return Content(rsp, "application/json");
				}
				else
				{
					var rsc = (int)presp.StatusCode;
					using (var fl2 = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Response.json"))
					{
						using var sw = new StreamWriter(fl2);
						await sw.WriteAsync($"Status code: {rsc}");
					}
					return StatusCode(rsc);
				}
			}
			catch (Exception e)
			{
				using var fle = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Exception.log");
				using var sw = new StreamWriter(fle);
				await sw.WriteAsync(e.ToString());
				logger.LogError(e, "Unhandled Exception");
				throw;
			}
		}

		private static readonly Regex rg = new(@"\{(\{[^\}]*\})\}", RegexOptions.Compiled);

		[HttpPost]
		[Route("{*url}")]
		public ValueTask<IActionResult> Wildcard(string url)
		{
			return Debug(url, s => rg.Replace(s, new MatchEvaluator(m => String.Format(m.Groups[1].Value, DateTime.Now))));
		}
	}
}
