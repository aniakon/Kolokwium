using System.ComponentModel.DataAnnotations;

namespace Kolokwium.Models.DTOs;

public class AppointmentAdderDTO
{
    [Required]
    public int appointmentId { get; set; }
    [Required]
    public int patientId { get; set; }
    [Required]
    public string pwz { get; set; }
    public List<ServicesAdderDTO> services { get; set; }
}

public class ServicesAdderDTO
{
    public string serviceName { get; set; }
    public decimal serviceFee { get; set; }
}