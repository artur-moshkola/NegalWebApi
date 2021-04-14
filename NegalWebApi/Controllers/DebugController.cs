using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NegalWebApi.Controllers
{
	[ApiController]
	public class DebugController : ControllerBase
	{
		private readonly ILogger logger;

		public DebugController(ILogger<DebugController> logger)
		{
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
				using (var fl1 = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Request.json"))
				{
					await Request.BodyReader.CopyToAsync(fl1);
				}
				string resp = null;
				if (!String.IsNullOrEmpty(dtid))
				{
					var rname = $"Data/{dtid}.json";
					if (!System.IO.File.Exists(rname)) rname = $"Data/Default.json";
					using (var fd = System.IO.File.OpenRead(rname))
					{
						using var sr = new StreamReader(fd);
						resp = await sr.ReadToEndAsync();
					}

					if (transform != null)
						resp = transform(resp);
				}

				if (resp != null)
				{
					using (var fl2 = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Response.json"))
					{
						using var sw = new StreamWriter(fl2);
						await sw.WriteAsync(resp);
					}
					return Content(resp, "application/json");
				}

				throw new Exception("Bad api path");
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
