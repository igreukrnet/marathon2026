using System.Runtime.CompilerServices;

namespace Restaurant_Aqa_Framework.Core;

public static class AllureInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        try
        {
            // Ensure parent directories exist in both AppContext.BaseDirectory and project root
            // so FileSystemResultsWriter.HasDirectoryAccess never encounters DirectoryNotFoundException.
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "TestResults"));
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "TestResults", "allure-results"));
            Directory.CreateDirectory(ArtifactPaths.Root);
            var resultsDir = ArtifactPaths.AllureResults;

            // Generate an absolute-path config for Allure.Net.Commons to write directly to TestResults/allure-results
            var generatedConfig = Path.Combine(ArtifactPaths.Root, "allureConfig.generated.json");
            var json = $$"""
            {
              "allure": {
                "directory": "{{resultsDir.Replace('\\', '/')}}"
              }
            }
            """;
            File.WriteAllText(generatedConfig, json);
            Environment.SetEnvironmentVariable("ALLURE_CONFIG", generatedConfig);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AllureInitializer] Failed to configure Allure environment: {ex.Message}");
        }
    }
}
