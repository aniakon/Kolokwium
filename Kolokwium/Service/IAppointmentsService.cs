using Kolokwium.Models.DTOs;

namespace Kolokwium.Service;

public interface IAppointmentsService
{
    public Task<AppointmentInfoDTO> GetAppointmentInfoAsync(int id);
    public Task AddAppointmentAsync(AppointmentAdderDTO appointment);
}