using Microsoft.Data.Sqlite;
using ResoniteWikiMine;
using ResoniteWikiMine.Commands;

if (args.Length < 1)
{
    Console.WriteLine("Pass a command, nerd");
    return 1;
}

var commandType = typeof(Program).Assembly.DefinedTypes.
    SingleOrDefault(x => x.IsAssignableTo(typeof(ICommand)) && x.Name == args[0]);

if (commandType == null)
{
    Console.WriteLine($"Unknown command: {args[0]}");
    return 1;
}

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.UserAgent);
using var dbConnection = new SqliteConnection($"Data Source={Constants.DbName}");
dbConnection.Open();

var context = new WorkContext(httpClient, dbConnection);

var command = (ICommand)Activator.CreateInstance(commandType)!;
return await command.Run(context, args[1..]);
