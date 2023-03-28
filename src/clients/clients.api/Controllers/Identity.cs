﻿using Clients.API.DTO;
using Clients.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace Clients.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class Identity : ControllerBase
    {
        private readonly IMediator _mediator;

        public Identity(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [HttpPost]
        [Route("/login")]
        [AllowAnonymous]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoginAsync([FromForm] ClientSigningRequest clientSigningRequest,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var signinRequest = new SigninRequest(clientSigningRequest.Username, clientSigningRequest.Password);

            var tokenResponse = await _mediator.Send(signinRequest, cancellationToken);
            
            return Ok(tokenResponse);
        }

        [HttpGet]
        public IActionResult TestAuthentication()
        {
            return Ok($"This is secured!");
        }
    }
}