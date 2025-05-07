namespace Kolokwium.Models.DTOs;

public class AppointmentInfoDTO
{
    public DateTime date { get; set; }
    public PatientInfoDTO patient { get; set; }
    public DoctorInfoDTO doctor { get; set; }
    public List<AppointmentServiceDTO> appointmentServices { get; set; }
}

public class PatientInfoDTO
{
    public string firstName { get; set; }
    public string lastName { get; set; }
    public DateTime dateOfBirth { get; set; }
}

public class DoctorInfoDTO
{
    public int doctorId { get; set; }
    public string pwz { get; set; }
}

public class AppointmentServiceDTO
{
    public string name { get; set; }
    public decimal serviceFee { get; set; }
}