using Cw7.DTO.Requests;
using Cw7.DTO.Responses;
using Cw7.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace Cw7.Services
{
    public class SqlServerStudentDbService : IStudentDbService
    {
        public EnrollStudentResponse EnrollStudent(EnrollStudentRequest request)
        {
            using (var connection = new SqlConnection("Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s17057;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                var response = new EnrollStudentResponse();
                command.Connection = connection;
                connection.Open();
                var tran = connection.BeginTransaction();
                command.Transaction = tran;
                command.CommandText = "SELECT IdStudy FROM studies WHERE name=@name";
                command.Parameters.AddWithValue("name", request.Studies);
                var dr = command.ExecuteReader();
                if (!dr.Read())
                {
                    dr.Close();
                    tran.Rollback();
                    throw new ArgumentException("Studia " + request.Studies + " nie isnieją");
                }
                int idstudies = (int)dr["IdStudy"];
                response.IdStudies = idstudies;
                response.Semester = 1;
                dr.Close();
                command.Parameters.Clear();
                command.CommandText = "SELECT TOP 1 IdEnrollment, StartDate FROM enrollment WHERE semester = 1 AND IdStudy = @idStudy order by StartDate desc";
                command.Parameters.AddWithValue("idStudy", idstudies);
                dr = command.ExecuteReader();
                if (!dr.Read())
                {
                    dr.Close();
                    command.CommandText = "INSERT INTO ENROLLMENT(IdEnrollment,Semester,IdStudy,StartDate) OUTPUT INSERTED.IdEnrollment VALUES((SELECT MAX(E.IdEnrollment) FROM Enrollment E) + 1,1,@idStudy,@startDate";
                    var studiesStartDate = DateTime.Now;
                    command.Parameters.AddWithValue("startDate", studiesStartDate);
                    dr = command.ExecuteReader();
                    dr.Read();
                    response.IdEnrollment = (int)dr["IdEnrollment"];
                    response.StartDate = studiesStartDate;
                }
                else
                {
                    response.IdEnrollment = (int)dr["IdEnrollment"];
                    response.StartDate = (DateTime)dr["StartDate"];
                }
                dr.Close();
                command.Parameters.Clear();
                command.CommandText = "INSERT INTO StudentAPBD(IndexNumber,FirstName,LastName,BirthDate,IdEnrollment) VALUES(@index,@fname,@lname,@bdate,@idenrollment)";
                command.Parameters.AddWithValue("index", request.IndexNumber);
                command.Parameters.AddWithValue("fname", request.FirstName);
                command.Parameters.AddWithValue("lname", request.LastName);
                command.Parameters.AddWithValue("bdate", request.BirthDate);
                command.Parameters.AddWithValue("idenrollment", response.IdEnrollment);
                try
                {
                    dr = command.ExecuteReader();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    dr.Close();
                    tran.Rollback();
                    throw new ArgumentException("Duplikat numeru indeksu");
                }
                response.IndexNumber = request.IndexNumber;
                dr.Close();
                tran.Commit();
                return response;
            }
        }

        public PromoteStudentsResponse PromoteStudents(PromoteStudentsRequest request)
        {
            using (var connection = new SqlConnection("Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s17057;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                connection.Open();
                command.CommandText = "EXEC PromoteStudents @Studies, @Semester";
                command.Parameters.AddWithValue("Studies", request.Studies);
                command.Parameters.AddWithValue("Semester", request.Semester);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    return new PromoteStudentsResponse
                    {
                        IdEnrollment = (int)dr["IdEnrollment"],
                        Semester = (int)dr["Semester"],
                        IdStudy = (int)dr["IdStudy"],
                        StartDate = (DateTime)dr["StartDate"]
                    };
                }
                else
                {
                    return null;
                }
            }
        }
        public Student GetStudent(String id)
        {

            using (var connection = new SqlConnection("Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s17057;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                var st = new Student();
                command.Connection = connection;
                connection.Open();
                command.CommandText = "SELECT * FROM StudentAPBD s LEFT JOIN ENROLLMENT e ON s.IdEnrollment = e.IdEnrollment LEFT JOIN STUDIES st on e.IdStudy = st.IdStudy WHERE IndexNumber LIKE @id";
                command.Parameters.AddWithValue("id", id);
                var dr = command.ExecuteReader();
                if (dr.Read())
                {
                    return new Student
                    {
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        IndexNumber = dr["IndexNumber"].ToString(),
                        BirthDate = Convert.ToDateTime(dr["BirthDate"].ToString()),
                        Studies = dr["Name"].ToString(),
                        Semester = Convert.ToInt32(dr["Semester"].ToString())
                    };
                }
                else
                {
                    return null;
                }
            }
        }
        public IEnumerable<Student> GetStudents()
        {

            using (var connection = new SqlConnection("Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s17057;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                var list = new List<Student>();
                command.Connection = connection;
                connection.Open();
                command.CommandText = "SELECT * FROM StudentAPBD s LEFT JOIN ENROLLMENT e ON s.IdEnrollment = e.IdEnrollment LEFT JOIN STUDIES st on e.IdStudy = st.IdStudy";
                var dr = command.ExecuteReader();
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        list.Add(new Student
                        {
                            FirstName = dr["FirstName"].ToString(),
                            LastName = dr["LastName"].ToString(),
                            IndexNumber = dr["IndexNumber"].ToString(),
                            BirthDate = Convert.ToDateTime(dr["BirthDate"].ToString()),
                            Studies = dr["Name"].ToString(),
                            Semester = Convert.ToInt32(dr["Semester"].ToString())
                        });
                    }
                    return list;
                }
                else
                {
                    return null;
                }

            }
        }
        public Boolean CheckStudent(String indexNumber)
        {
            using (var connection = new SqlConnection("Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s17057;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                connection.Open();
                command.CommandText = "SELECT * FROM StudentAPBD WHERE IndexNumber LIKE @indexNumber";
                command.Parameters.AddWithValue("indexNumber", indexNumber);
                var dr = command.ExecuteReader();
                return dr.HasRows;
            }
        }
    }
}
