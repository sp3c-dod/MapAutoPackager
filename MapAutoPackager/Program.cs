using MapPackager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapAutoPackager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // The following files are in the stock BSPs, but not included with the game:
            // dod_falaise.bsp: sound\ambience\frenchcountry.wav
            // dod_glider.bsp: sound\ambience\moo.wav
            // dod_saints.bsp: sound\ambience\opera.wav
            string[] includedBspNames = new string[]
            {
                "dod_anzio.bsp",
                "dod_avalanche.bsp",
                "dod_caen.bsp",
                "dod_charlie.bsp",
                "dod_chemille.bsp",
                "dod_donner.bsp",
                "dod_escape.bsp",
                "dod_falaise.bsp",
                "dod_flash.bsp",
                "dod_flugplatz.bsp",
                "dod_forest.bsp",
                "dod_glider.bsp",
                "dod_jagd.bsp",
                "dod_kalt.bsp",
                "dod_kraftstoff.bsp",
                "dod_merderet.bsp",
                "dod_northbound.bsp",
                "dod_saints.bsp",
                "dod_sturm.bsp",
                "dod_switch.bsp",
                "dod_vicenza.bsp",
                "dod_zalec.bsp"
            };

            // Game search order for files
            //1st: /Half-Life/dod/
            //2nd: /Half-Life/dod_downloads/
            //3rd: /Half-Life/valve/

            const string dodDownloadsMapsDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Half-Life\dod_downloads\maps\";
            const string dodDownloadsDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Half-Life\dod_downloads\";
            const string gameDirectoy = @"C:\Program Files (x86)\Steam\steamapps\common\Half-Life\dod\";
            const string zipOutputPath = @"C:\Users\Bill\Documents\DOD\Maps\Classic Maps\Auto-Packaged\";
            const string resultsOutputPath = @"C:\Users\Bill\Documents\DOD\Maps\Classic Maps\Auto-Packaged\Results Output\{0}";
            //const string zipOutputPath = @"C:\temp\Map Pack Test\";
            //const string resultsOutputPath = @"C:\temp\Map Pack Test\Results Output\{0}";

            Packager mapPackager = new Packager()
            {
                GameDirectories = new string[] { gameDirectoy, dodDownloadsDirectory },
                ZipOutputDirectory = zipOutputPath,
                PathToResGenExecutable = @"C:\Users\Bill\source\repos\MapAutoPackager\MapPackager\ResGen",
                PutMapsWithErrorsInSubFolders = true
            };

            // Gather all BSP names in the dod and dod_downloads folder except the maps that come with the game
            var dodDownloadsFolderBsps = Directory.GetFiles(dodDownloadsMapsDirectory, "*.bsp", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f)).ToList();
            var dodFolderBsps = Directory.GetFiles(gameDirectoy + "maps\\", "*.bsp", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f)).ToList();
            var dodBspsExcludingBuiltIn = dodFolderBsps.Except(includedBspNames).ToList();
            var bspsToPackage = dodDownloadsFolderBsps.Union(dodBspsExcludingBuiltIn).ToList();

            //var duplicates = dodDownloadsFolderBsps.Intersect(dodBspsExcludingBuiltIn).ToList();  // For finding duplicates between the dod and dod_downloads folders
            //var bspsToPackage = new string[] { "dod_hostile.bsp" };  // Use this to package a single map

            // Package Map
            MapPackageResult result;
            foreach (string bspName in bspsToPackage)
            //foreach (string bspName in includedBspNames)
            {
                // If the output or error output already exists then skip this file. This is so we can pick up where we left off
                // if there is an unhandled exception
                if (File.Exists(String.Format(resultsOutputPath, bspName.Replace(".bsp", ".csv"))) ||
                    File.Exists(String.Format(resultsOutputPath, bspName.Replace(".bsp", " - ERROR.csv"))))
                {
                    continue;
                }

                List<string> packagingReport = new List<string>();
                Console.WriteLine($"Packaging {bspName}...");
                result = mapPackager.Package(bspName);
  
                // Display output
                if (result.ZipCreationSuccesful)
                {
                    Console.WriteLine($"{result.MapName} Pacakge Succesfully Created at {result.ZipFilePath}");
                    Console.WriteLine("Included Files: ");
                    packagingReport.Add("File Name, File Importance, File Exists, Duplicate With Differences Found");
                    foreach (var file in result.AssociatedFiles)
                    {
                        packagingReport.Add($"{file.FileName}, {file.FileImportance}, {file.Exists}, {file.DuplicateWithDifferencesFound}");
                        if (file.Exists)
                        {
                            Console.WriteLine($"... {file.FileName}");
                        }
                    }

                    File.AppendAllLines(String.Format(resultsOutputPath, bspName.Replace(".bsp", ".csv")), packagingReport.ToArray());
                }
                else
                {
                    Console.WriteLine($"ZIP Creation of {bspName} was NOT succesful");
                    Console.WriteLine($"Error: {result.ErrorMessage}");
                    packagingReport.Add($"Error: {result.ErrorMessage}");
                    packagingReport.Add("File Name, File Importance, File Exists, Duplicate With Differences Found");
                    foreach (var file in result.AssociatedFiles)
                    {
                        packagingReport.Add($"{file.FileName}, {file.FileImportance}, {file.Exists}, {file.DuplicateWithDifferencesFound}");
                    }

                    File.AppendAllLines(String.Format(resultsOutputPath, bspName.Replace(".bsp", " - ERROR.csv")), packagingReport.ToArray());
                }

                // Add a blank line between maps
                Console.WriteLine();
            }
        }
    }
}
