using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapPackager;

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
            const string zipOutputPath = @"C:\temp";
            const string resultsOutputPath = @"C:\temp\{0}";

            Packager mapPackager = new Packager()
            {
                GameDirectories = new string[] { gameDirectoy, dodDownloadsDirectory },
                ZipOutputDirectory = zipOutputPath,
                PathToResGenExecutable = @"C:\Users\Bill\source\repos\MapAutoPackager\MapPackager\ResGen"
            };

            // Gather all BSP names in the dod_downloads folder
            var allBsps = Directory.GetFiles(dodDownloadsMapsDirectory, "*.bsp", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f)).ToList();

            // Package Map
            MapPackageResult result;
            //TODO: foreach (string bspName in allBsps)
            foreach (string bspName in includedBspNames)
            {
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
                }
                else
                {
                    Console.WriteLine($"ZIP Creation of {bspName} was NOT succesful");
                    Console.WriteLine($"Error: {result.ErrorMessage}");
                }

                File.AppendAllLines(String.Format(resultsOutputPath, bspName.Replace(".bsp", ".csv")), packagingReport.ToArray());

                // Add a blank line between maps
                Console.WriteLine();
            }
        }
    }
}
