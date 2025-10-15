using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace SkOfflineCourse.Plugins
{
    public class WellhubDataPlugin
    {
        private readonly string _dataPath = "data";

        [KernelFunction]
        [Description("Obtém todos os usuários do sistema")]
        public string GetUsers()
        {
            try
            {
                var filePath = Path.Combine(_dataPath, "users.json");
                if (!File.Exists(filePath)) return "[]";
                
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler usuários: {ex.Message}");
                return "[]";
            }
        }

        [KernelFunction]
        [Description("Obtém todos os registros de check-in")]
        public string GetCheckinRecords()
        {
            try
            {
                var filePath = Path.Combine(_dataPath, "checkin_records.json");
                if (!File.Exists(filePath)) return "[]";
                
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler check-ins: {ex.Message}");
                return "[]";
            }
        }

        [KernelFunction]
        [Description("Encontra um usuário específico por email")]
        public string FindUser([Description("Email do usuário")] string email)
        {
            try
            {
                var users = GetUsers();
                var userArray = JsonSerializer.Deserialize<JsonElement[]>(users);
                
                var user = userArray?.FirstOrDefault(u => 
                    u.TryGetProperty("email", out var emailProp) && 
                    emailProp.GetString() == email);
                
                return user?.ToString() ?? "null";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao encontrar usuário: {ex.Message}");
                return "null";
            }
        }

        [KernelFunction]
        [Description("Filtra usuários que falharam em check-ins")]
        public string GetFailedCheckins()
        {
            try
            {
                var checkins = GetCheckinRecords();
                var checkinArray = JsonSerializer.Deserialize<JsonElement[]>(checkins);
                
                var failed = checkinArray?.Where(c =>
                    c.TryGetProperty("status", out var status) &&
                    status.GetString() == "failed").ToArray();
                
                return JsonSerializer.Serialize(failed ?? new JsonElement[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao filtrar check-ins falhos: {ex.Message}");
                return "[]";
            }
        }
    }
}