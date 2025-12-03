using InterdisciplinairProject.Core.Models;
using System;
using System.IO;
using System.Text.Json;

namespace Show
{
    public class SceneExtractor
    {
        public static Scene ExtractScene(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"The selected file could not be found: {jsonFilePath}");
            }

            try
            {
                string jsonString = File.ReadAllText(jsonFilePath);

                var scene = JsonSerializer.Deserialize<Scene>(
                    jsonString,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

                return scene ?? throw new InvalidDataException("Failed to deserialize the JSON into a Scene object.");
            }
            catch (JsonException ex)
            {
                throw new JsonException($"The file contains invalid JSON: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                throw new IOException($"Error reading the file: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred: {ex.Message}", ex);
            }
        }
    }
}