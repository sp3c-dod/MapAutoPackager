using System;
using MapPackager;

namespace MapAutoPackager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            const string bspName = "aac_true_gold.bsp";
            const string gameDirectoy = @"C:\Program Files (x86)\Steam\steamapps\common\Half-Life\dod_downloads\maps";
            const string zipOutputPath = @"C:\temp";

            Packager mapPackager = new Packager()
            {
                GameDirectories = new string[] { gameDirectoy },
                ZipOutputDirectory = zipOutputPath,
                PathToResGenExecutable = @"C:\Users\Bill\source\repos\MapAutoPackager\MapPackager\ResGen"
            };

            // TODO:
            // Loop through directory of BSPs
            // Package Map
            var result = mapPackager.Package(bspName);

            // Display output

        }
    }
}
