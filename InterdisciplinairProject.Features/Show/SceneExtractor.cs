using InterdisciplinairProject.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Extracts multiple scenes from a JSON file. Supports both array format and single scene format.
        /// </summary>
        /// <param name="jsonFilePath">Path to the JSON file.</param>
        /// <returns>List of scenes extracted from the file.</returns>
        public static List<Scene> ExtractScenes(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"The selected file could not be found: {jsonFilePath}");
            }

            try
            {
                string jsonString = File.ReadAllText(jsonFilePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                // Check if root is an array
                if (root.ValueKind == JsonValueKind.Array)
                {
                    var scenes = new List<Scene>();
                    
                    foreach (var element in root.EnumerateArray())
                    {
                        try
                        {
                            var sceneJson = element.GetRawText();
                            var scene = JsonSerializer.Deserialize<Scene>(sceneJson, options);
                            
                            if (scene != null)
                            {
                                scenes.Add(scene);
                            }
                        }
                        catch (JsonException)
                        {
                            // Skip this scene and continue with others
                            continue;
                        }
                    }

                    return scenes;
                }
                else
                {
                    // Single scene - deserialize and return as list
                    var scene = JsonSerializer.Deserialize<Scene>(jsonString, options);
                    return scene != null ? new List<Scene> { scene } : new List<Scene>();
                }
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