// See https://aka.ms/new-console-template for more information

using MySqlConnector;
using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 5000);

server.Start();

Console.WriteLine("Server started on port 5000...");
Console.WriteLine("Waiting for client...");

TcpClient client = server.AcceptTcpClient();

Console.WriteLine("Client connected!");

NetworkStream stream = client.GetStream();

byte[] buffer = new byte[1024];
int bytesRead = stream.Read(buffer, 0, buffer.Length);

string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

Console.WriteLine("Message from client: " + message);

string response;

if (message.StartsWith("LOGIN|"))
{
    string[] parts = message.Split('|');

    if (parts.Length == 3)
    {
        string username = parts[1];
        string password = parts[2];

        Console.WriteLine("Username: " + username);

        string connectionString =
            Environment.GetEnvironmentVariable("NIGHTSKY_DB")
            ?? throw new Exception("NIGHTSKY_DB is not configured");

        using MySqlConnection connection = new MySqlConnection(connectionString);
        connection.Open();

        string query = @"
    SELECT COUNT(*)
    FROM users
    WHERE username = @username
    AND password = @password;
";

        using MySqlCommand command = new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@password", password);

        long count = Convert.ToInt64(command.ExecuteScalar());

        if (count > 0)
        {
            response = "LOGIN_OK";
        }
        else
        {
            response = "LOGIN_FAILED";
        }
    }
    else
    {
        response = "INVALID_LOGIN_DATA";
    }
}
else
{
    response = "UNKNOWN_REQUEST";
}

byte[] responseBytes = Encoding.UTF8.GetBytes(response);
stream.Write(responseBytes, 0, responseBytes.Length);

client.Close();
server.Stop();

Console.WriteLine("Server stopped.");
