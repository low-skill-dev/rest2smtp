using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using services;
using System.ComponentModel.DataAnnotations;

namespace api.Controllers;

[ApiController]
[AllowAnonymous]
[Consumes("application/json")]
[Produces("application/json")]
[Route("/api/[controller]")]
public sealed class MailController:ControllerBase
{
	private readonly SmtpService _smtp;
	private readonly LoadBalancerService _balancer;
	public MailController(SmtpService smtp, LoadBalancerService balancer)
	{
		_smtp = smtp;
		_balancer = balancer;
	}

	[HttpGet]
	[Route("limits")]
	public IActionResult GetCurrentLimitations()
	{
		return Ok(new LimitationsResponse {
			TotalPerDayLimit = _balancer.TotalLimitPerDay,
			CanSendImmediately = _balancer.CanSendImmediately,
			SendedLast24Hours = _balancer.TotalLimitPerDay 
				- _balancer.CanSendImmediately - LoadBalancerService.SafetyGap
		});;;
	}

	[HttpPost]
	public async Task<IActionResult> SendEmail([FromBody][Required] SendMailRequest request)
	{
		var sended = await _smtp.Send(request);

		return StatusCode(sended ? 200 : 500);
	}
}
