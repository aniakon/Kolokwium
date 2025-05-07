using Kolokwium.Models.DTOs;
using System.Data;
using System.Data.Common;
using Kolokwium.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;


namespace Kolokwium.Service;

public class AppointmentsService : IAppointmentsService
{
    IConfiguration _configuration;

    public AppointmentsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<AppointmentInfoDTO> GetAppointmentInfoAsync(int id)
    {
        string query = @"SELECT a.date, p.first_name, p.last_name, p.date_of_birth, d.doctor_id, d.PWZ, s.name, s.base_fee 
         FROM Appointment a 
         JOIN Patient p ON p.patient_id = a.patient_id 
         JOIN Doctor d ON d.doctor_id = a.doctor_id 
         JOIN Appointment_Service ase ON a.appoitment_id = ase.appoitment_id 
         JOIN Service s ON s.service_id = ase.service_id 
         WHERE a.appoitment_id = @id";

        await using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
        await using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@id", id);
            
            await connection.OpenAsync();
            
            var reader = await command.ExecuteReaderAsync();
            var result = new AppointmentInfoDTO();
            bool firstRead = true;
            
            while (await reader.ReadAsync())
            {
                if (firstRead)
                {
                    result.date = reader.GetDateTime(0);
                    result.patient = new PatientInfoDTO()
                    {
                        firstName = reader.GetString(1),
                        lastName = reader.GetString(2),
                        dateOfBirth = reader.GetDateTime(3),
                    };
                    result.doctor = new DoctorInfoDTO()
                    {
                        doctorId = reader.GetInt32(4),
                        pwz = reader.GetString(5)
                    };
                    result.appointmentServices = new List<AppointmentServiceDTO>()
                    {
                        new AppointmentServiceDTO()
                        {
                            name = reader.GetString(6),
                            serviceFee = reader.GetDecimal(7)
                        }
                    };
                    firstRead = false;
                }
                else
                {
                    result.appointmentServices.Add(new AppointmentServiceDTO()
                    {
                        name = reader.GetString(6),
                        serviceFee = reader.GetDecimal(7)
                    });
                }
            }
            await reader.CloseAsync();
            await connection.CloseAsync();
            if (firstRead) throw new NotFoundException("Nie ma wizyty o podanym ID w bazie.");
            return result;
        }
    }

    public async Task AddAppointmentAsync(AppointmentAdderDTO givenAppointment)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        
        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            // sprawdzam czy istnieje wizyta o danym id
            command.Parameters.Clear();
            command.CommandText = @"SELECT 1 FROM Appointment WHERE appoitment_id = @id";
            command.Parameters.AddWithValue("@id", givenAppointment.appointmentId);

            await connection.OpenAsync();
            var res = await command.ExecuteScalarAsync();
            if (res != null) throw new NotFoundException("Już istnieje wizyta o podanym identyfikatorze.");
            
            // sprawdzam czy istnieje pacjent o danym id
            command.Parameters.Clear();
            command.CommandText = @"SELECT 1 FROM Patient WHERE patient_id = @patientId";
            command.Parameters.AddWithValue("@patientId", givenAppointment.patientId);
            var res1 = await command.ExecuteScalarAsync();
            if (res1 == null) throw new NotFoundException("Nie ma pacjenta o podanym identyfikatorze.");

            
            // sprawdzam czy istnieje lekarz o podanym numerze PWZ
            command.Parameters.Clear();
            command.CommandText = @"SELECT 1 FROM Doctor WHERE PWZ = @doctorPwz";
            command.Parameters.AddWithValue("@doctorPwz", givenAppointment.pwz);
            var res2 = await command.ExecuteScalarAsync();
            if (res2 == null) throw new NotFoundException("Nie ma lekarza o podanym numerze PWZ.");

            // wstawiam wizytę
            command.Parameters.Clear();
            command.CommandText = @"INSERT INTO Appointment VALUES (@appointmentId, @patientId, (SELECT doctor_id FROM Doctor WHERE PWZ = @doctorPwz), @date); SELECT SCOPE_IDENTITY()";
            command.Parameters.AddWithValue("@appointmentId", givenAppointment.appointmentId);
            command.Parameters.AddWithValue("@patientId", givenAppointment.patientId);
            command.Parameters.AddWithValue("@doctorPwz", givenAppointment.pwz);
            command.Parameters.AddWithValue("@date", DateTime.Now);
            int appointmentGotId = -1;
            
            try
            {
                var res3 = await command.ExecuteScalarAsync();
                appointmentGotId = Convert.ToInt32(res3);
            }
            catch (Exception e)
            {
                throw new CannotAddException("Taka wizyta już jest w bazie.");
            }

            foreach (ServicesAdderDTO service in givenAppointment.services)
            {
                command.Parameters.Clear();
                command.CommandText = @"SELECT 1 FROM Service WHERE name = @serviceName";
                command.Parameters.AddWithValue("@serviceName", service.serviceName);
                var res4 = await command.ExecuteScalarAsync();
                if (res4 == null) throw new NotFoundException("Nie ma serwisu u podanej nazwie");
                
                command.Parameters.Clear();
                command.CommandText =
                    "INSERT INTO Appointment_Service VALUES (@appointmentId, (SELECT service_id FROM Service WHERE name = @serviceName), @serviceFee)";
                command.Parameters.AddWithValue("@appointmentId", givenAppointment.appointmentId);
                command.Parameters.AddWithValue("@serviceName", service.serviceName);
                command.Parameters.AddWithValue("@serviceFee", service.serviceFee);

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    throw new CannotAddException("Błąd przy wstawianiu serwisu.");
                }
            }
            await transaction.CommitAsync();
            await connection.CloseAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            await connection.CloseAsync();
            throw;
        }
    }
}