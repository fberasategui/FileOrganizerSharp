using System.Reflection;
using System.Text;
using System.Text.Json;

var getConfig = () =>
{
    var configFile = "fileorganizer.config.json";
    if (File.Exists(configFile))
    {
        var config = File.ReadAllText(configFile);
        return JsonSerializer.Deserialize<Configuration>(config);
    }
    else
    {
        using var stream = Assembly.GetEntryAssembly().GetManifestResourceStream("FileOrganizer.defaultconfig.json");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var config = reader.ReadToEnd();
        File.WriteAllText(configFile, config);
        return JsonSerializer.Deserialize<Configuration>(config);
    }
};

var input = (string prompt) =>
{
    Console.WriteLine(prompt);
    return Console.ReadKey();
};

var createFolder = (string name) => Directory.CreateDirectory(name);

var moveFileToFolder = (string filePath, string folderName) =>
{
    var fileName = Path.GetFileName(filePath);
    if (Path.GetDirectoryName(filePath) == folderName)
        return false;
    var destinationPath = Path.Combine(folderName, fileName);
    if (File.Exists(destinationPath))
    {
        var userInput = input($"El archivo {filePath} ya existe en la carpeta {folderName}. ¿Desea Sobreescribirlo? (y/n)");
        if (userInput.Key != ConsoleKey.Y)
            return false;
    }
    createFolder(folderName);
    File.Move(filePath, destinationPath);

    Console.WriteLine($"El archivo {fileName} se movió a la carpeta {folderName}");
    return true;
};

var config = getConfig();

var organizeFiles = (string directory) =>
{
    var extensions =
        from f in config.fileTypes
        from e in f.Value
        select (e, f.Key);

    var dict = 
        extensions.DistinctBy(x => x.e)
                  .ToDictionary(x => x.e, x => x.Key);

    var totalFilesMoved = 0;
    foreach (var file in Directory.GetFiles(directory))
    {
        if (file == Assembly.GetEntryAssembly().Location)
            continue;

        if (config.exclusions.Contains(Path.GetFileName(file)))
            continue;

        var extension = Path.GetExtension(file)[1..].ToLower();

        if (dict.TryGetValue(extension, out var folder))
        {
            moveFileToFolder(file, Path.Combine(directory, folder));
            totalFilesMoved++;
        }
    }
    return totalFilesMoved;
};

var totalFilesMoved = organizeFiles(Directory.GetCurrentDirectory());
if (totalFilesMoved == 0)
    Console.WriteLine("No se movió ningún archivo.");
else
    Console.WriteLine($"Se movieron {totalFilesMoved} archivos.");

record Configuration(string[] exclusions, Dictionary<string, string[]> fileTypes);
