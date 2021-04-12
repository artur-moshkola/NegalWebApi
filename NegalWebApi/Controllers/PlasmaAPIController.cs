using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NegalWebApi.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class PlasmaAPIController : ControllerBase
	{
		[HttpPost]
		public IActionResult GetServerTime()
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		public IActionResult GetOperatorInfo()
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		public IActionResult GetOperatorPhoto()
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		public IActionResult GetDonorsInfo()
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		public IActionResult GetDonorsPhoto()
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		public IActionResult GetEquipments()
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		public IActionResult GetSodiumChlorides()
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		public IActionResult GetAnticoagulants()
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		public IActionResult SavePlasmaCollection()
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		public IActionResult SaveException()
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		public IActionResult GetPlasmaParmas()
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		public IActionResult UploadPlasmaParams()
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		public IActionResult UploadPlasmaComment()
		{
			throw new NotImplementedException();
		}
	}
}
