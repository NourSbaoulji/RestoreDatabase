using System.Data.SqlClient;
using System.Xml.Linq;
namespace Script_for_restoring_db;
file class Program
{
    static void Main(string[] args)
    {
        string serverName = "tcp:localhost,1433";
        string username = "sa";
        string password = "pbas01!123";
        string connectionString = $"Server={serverName};User Id={username};Password={password};";
        const string backupFilePath = "C:\\Users\\Nour Sbaoulji\\Desktop\\Projects\\DrawbridgeSetUp\\DrawbridgeSetUp\\.data\\mssql-backup\\Backup";
        Console.WriteLine("Enter your choice: \r\n    None : 0\r\n    Demo : 1\r\n    Development : 2,r\n    Live : 3\r\n    QA : 4\r\n    UAT : 5 ");
        var env = Int32.Parse(Console.ReadLine());
        DbClient.RestoreDatabase(connectionString, backupFilePath, (Environment)env);
    }
}
file static class DbClient
{
    public static void RestoreDatabase(string connectionString, string path, Environment environment)
    {
        var subEnvironment = new List<string>
    {
        "Dev",
        "Development",
        "Local",
        "Live",
        "UAT",
        "Demo",
        "QA"
    };
        var dbNames = GetDbNames(path);
        var env = environment == Environment.None ? "" : environment.ToString(); 
        List<string> simplifiedNames = new();
        List<string> rowNames = new();
        foreach (var dbName in dbNames)
        {
            simplifiedNames.Add(dbName.Replace(".bak", ""));
        }
        foreach (var name in simplifiedNames)
        {
            bool endsWithSubEnv = false;
            foreach (var sub in subEnvironment)
            {
                if (name.EndsWith(sub))
                {
                    var rowName = name.Substring(0, name.LastIndexOf(".")); 
                    rowNames.Add(env == "" ? rowName : $"{rowName}.{env}"); 
                    endsWithSubEnv = true;
                    break;
                }
            }
            if (!endsWithSubEnv)
            {
                rowNames.Add(name);
            }
        }

        if (simplifiedNames.Contains("BrokersClaims"))
        {
            var index = simplifiedNames.IndexOf("BrokersClaims");
            simplifiedNames[index] = "Claims";
        }
        if (rowNames.Contains("BrokersClaims"))
        {
            var index = rowNames.IndexOf("BrokersClaims");
            rowNames[index] = "Claims";
        }

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            for (int i = 0; i < dbNames.Count; i++)
            {
                var moveCommand = "";
                var rowName = rowNames[i];
                if (!string.IsNullOrEmpty(env))
                {
                    moveCommand = $", MOVE '{simplifiedNames[i]}' TO '/var/opt/mssql/data/{rowName}.mdf', MOVE '{simplifiedNames[i]}_log' TO '/var/opt/mssql/data/{rowName}.ldf'";
                }
                command.CommandText = $@"
                                    RESTORE DATABASE [{rowName}]
                                    FROM DISK = '/var/opt/mssql/data/Backup/{dbNames[i]}'
                                    WITH REPLACE{moveCommand};";
                command.ExecuteNonQuery();
            }
            connection.Close();
        }
    }


    private static List<string> GetDbNames(string path)
    {
        var fileList = new List<string>();
        try
        {
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    fileList.Add(Path.GetFileName(file));
                }
            }
            else
            {
                Console.WriteLine("The specified folder does not exist.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
        var bakFiles = fileList.Where(s => s.EndsWith(".bak")).ToList();
        return bakFiles;
    }
}
enum Environment
{
    None=0,
    Demo = 1,
    Development=2,
    Live=3,
    QA=4,
    UAT=5
}