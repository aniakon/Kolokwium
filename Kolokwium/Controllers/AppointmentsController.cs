using Kolokwium.Exceptions;
using Kolokwium.Models.DTOs;
using Kolokwium.Service;
using Microsoft.AspNetCore.Mvc;

namespace Kolokwium.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentsService _appointmentsService;

    public AppointmentsController(IAppointmentsService appointmentsService)
    {
        _appointmentsService = appointmentsService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentInfoDTO>> GetAppointmentInfoById(int id)
    {
        try
        {
            var res = await _appointmentsService.GetAppointmentInfoAsync(id);
            return Ok(res);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddAppointmentAsync(AppointmentAdderDTO appointment)
    {
        try
        {
            await _appointmentsService.AddAppointmentAsync(appointment);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (CannotAddException e)
        {
            return Conflict(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
        return CreatedAtAction(nameof(GetAppointmentInfoById), new { id = appointment.appointmentId }, appointment);
    }
}