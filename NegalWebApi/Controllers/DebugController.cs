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
		private async ValueTask<IActionResult> Debug(string method, Func<string, string> transform = null)
		{
			try
			{
				using (var fl1 = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Request.json"))
				{
					await Request.BodyReader.CopyToAsync(fl1);
				}
				string resp;
				var rname = $"Data/{method}.json";
				if (!System.IO.File.Exists(rname)) rname = $"Data/Default.json";
				using (var fd = System.IO.File.OpenRead(rname))
				{
					using var sr = new StreamReader(fd);
					resp = await sr.ReadToEndAsync();
				}

				if (transform != null)
					resp = transform(resp);

				using (var fl2 = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Response.json"))
				{
					using var sw = new StreamWriter(fl2);
					await sw.WriteAsync(resp);
				}

				return Content(resp, "application/json");
			}
			catch (Exception e)
			{
				using var fle = System.IO.File.OpenWrite($"Logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{method}_Exception.log");
				using var sw = new StreamWriter(fle);
				await sw.WriteAsync(e.ToString());
				throw;
			}
		}

		private static readonly Regex rg = new(@"\{(\{[^\}]*\})\}", RegexOptions.Compiled);

		[HttpPost]
		[Route("{*url}")]
		public ValueTask<IActionResult> Wildcard(string url)
		{
			return Debug(url.Replace('/','-'), s => rg.Replace(s, new MatchEvaluator(m => String.Format(m.Groups[1].Value, DateTime.Now))));
		}
	}
}
