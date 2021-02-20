using CommonModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace PatientProvider.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly MyConfiguration _myConfiguration;

        /// <summary>
        /// Please set the dbLocation for demo Sqlite files in appsettings.json file.
        /// Please set the appropriated smtp configs in appsettings.json file for email notifiaction sending.
        /// </summary>
        /// <param name="myConfiguration"></param>
        public PatientsController(IOptions<MyConfiguration> myConfiguration)
        {
            _myConfiguration = myConfiguration.Value;
        }

        [HttpGet("use_cases")]
        public Dictionary<string, string> GetUseCases()
        {
            var result = new Dictionary<string, string>();

            string dbPath = Path.Combine(_myConfiguration.DbLocation, "use_cases.db");

            using (var con = new SQLiteConnection($"URI=file:{dbPath}"))
            {
                con.Open();

                using(var cmd = new SQLiteCommand(con))
                {
                    cmd.CommandText = "SELECT use_case_id, use_case_name FROM use_cases;";

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            result.Add(reader.GetInt32(0).ToString(), reader.GetString(1));
                        }
                    }
                }
            }

            return result;
        }

        [HttpGet("use_case_result")]
        public List<Measurement> GetUsesCaseResult(int useCaseId)
        {
            var result = new List<Measurement>();

            if (useCaseId == 4)
            {
                string tableName = "";
                string useCasesDbPath = Path.Combine(_myConfiguration.DbLocation, "use_cases.db");

                using (var con = new SQLiteConnection($"URI=file:{useCasesDbPath}"))
                {
                    con.Open();

                    using (var cmd = new SQLiteCommand(con))
                    {
                        cmd.CommandText = $"SELECT table_name FROM use_case_from WHERE use_case_id = {useCaseId};";

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tableName = reader.GetString(0);
                                break;
                            }
                        }
                    }
                }

                string tableNameDbPath = Path.ChangeExtension(Path.Combine(_myConfiguration.DbLocation, tableName), "db");
                using (var con = new SQLiteConnection(string.Format($"URI=file:{tableNameDbPath}", tableName)))
                {
                    con.Open();

                    using (var cmd = new SQLiteCommand(con))
                    {

                        cmd.CommandText = $"SELECT measurement_id, measurement_date, measurement FROM {tableName}";

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var measurement = new Measurement
                                {
                                    MeasurementId = reader.GetInt32(0),
                                    MeasurementDate = reader.GetString(1),
                                    MeasurementValue = reader.GetFloat(2),
                                };

                                measurement.IsProblem = measurement.MeasurementValue >= 7;

                                result.Add(measurement);
                            }
                        }
                    }
                }

            }

            return result;
        }

        [HttpGet("send_notification")]
        public void SendNotification(int useCaseId, int measurementId)
        {
            if (useCaseId == 4)
            {
                string patientBloodSugarDbPath = Path.Combine(_myConfiguration.DbLocation, "patient_blood_sugars.db");
                int patientId = 0;
                using (var con = new SQLiteConnection($"URI=file:{patientBloodSugarDbPath}"))
                {
                    con.Open();

                    using (var cmd = new SQLiteCommand(con))
                    {
                        cmd.CommandText = $"SELECT patient_id FROM patient_blood_sugars WHERE measurement_id = {measurementId};";

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                patientId = reader.GetInt32(0);
                                break;
                            }
                        }
                    }
                }

                string patientDbPath = Path.Combine(_myConfiguration.DbLocation, "patients.db");
                string email = "";
                using (var con = new SQLiteConnection($"URI=file:{patientDbPath}"))
                {
                    con.Open();

                    using (var cmd = new SQLiteCommand(con))
                    {
                        cmd.CommandText = $"SELECT email FROM patients WHERE patient_id = {patientId};";

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                email = reader.GetString(0);
                                break;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(email))
                {
                    var smtpClient = new SmtpClient(_myConfiguration.SmtpClient)
                    {
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(_myConfiguration.UserForMailSending, _myConfiguration.PasswordForMailSending),
                    };

                    smtpClient.Send(_myConfiguration.UserForMailSending, email, "Medical Examination", "Go to a medical examination please.");
                }

            }
        }

    }
}
