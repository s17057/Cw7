using Cw7.DTO.Requests;
using Cw7.DTO.Responses;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Cw7.Services
{
    public class JwtAuthorizationService : IJwtAuthorizationService
    {
        private IConfiguration _configuration { get; set; }
        public JwtAuthorizationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public JwtResponse LogIn(LoginRequest request)
        {
            using (var connection = new SqlConnection("Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s17057;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                connection.Open();
                var tran = connection.BeginTransaction();
                command.Transaction = tran;
                command.CommandText = "SELECT pswd, salt FROM StudentAPBD WHERE IndexNumber = @index";
                command.Parameters.AddWithValue("index", request.Index);
                var dr = command.ExecuteReader();
                if (dr.Read())
                {
                    var salt = (byte[])dr["salt"];
                    var pswd = dr["pswd"].ToString();
                    dr.Close();
                    if (PasswordHelper.CheckPswd(request.Password, pswd, salt))
                    {
                        var claims = new[]
                        {
                            new Claim("Index",request.Index),
                            new Claim(ClaimTypes.Role,"Student")
                        };
                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["SecretKey"]));
                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                        var token = new JwtSecurityToken
                        (
                            issuer: "Gakko",
                            audience: "Students",
                            claims: claims,
                            expires: DateTime.Now.AddMinutes(10),
                            signingCredentials: creds
                        );
                        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                        command.CommandText = "UPDATE StudentAPBD SET refreshToken = @refreshToken WHERE IndexNumber = @index";
                        command.Parameters.AddWithValue("refreshToken", refreshToken);
                        command.ExecuteNonQuery();
                        tran.Commit();
                        return new JwtResponse
                        {
                            Token = new JwtSecurityTokenHandler().WriteToken(token),
                            RefreshToken = refreshToken
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }
        public JwtResponse RefreshToken(String refreshToken)
        {
            using (var connection = new SqlConnection("Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s17057;Integrated Security=True"))
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                connection.Open();
                command.CommandText = "SELECT IndexNumber FROM StudentAPBD WHERE refreshToken = @refreshToken";
                command.Parameters.AddWithValue("refreshToken", refreshToken);
                var dr = command.ExecuteReader();
                if (dr.Read())
                {
                    var index = dr["IndexNumber"].ToString();
                    dr.Close();
                    var claims = new[]
                    {
                            new Claim("Index",index),
                            new Claim(ClaimTypes.Role,"student")
                        };
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["SecretKey"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var token = new JwtSecurityToken
                    (
                        issuer: "Gakko",
                        audience: "Students",
                        claims: claims,
                        expires: DateTime.Now.AddMinutes(10),
                        signingCredentials: creds
                    );
                    var newRefreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                    command.Parameters.Clear();
                    command.CommandText = "UPDATE StudentAPBD SET refreshToken = @refreshToken WHERE IndexNumber = @index";
                    command.Parameters.AddWithValue("refreshToken", refreshToken);
                    command.Parameters.AddWithValue("index", index);
                    command.ExecuteNonQuery();

                    return new JwtResponse
                    {
                        Token = new JwtSecurityTokenHandler().WriteToken(token),
                        RefreshToken = refreshToken
                    };
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
