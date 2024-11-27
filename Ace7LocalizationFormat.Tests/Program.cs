using Ace7LocalizationFormat.Formats;

namespace Ace7LocalizationFormat.Tests
{
    class Program
    {
        public static void Main(string[] args)
        {
            string cmnPath = "E:\\MODDING\\_Ace Combat 7\\_tools\\UnrealPak_Enhanced\\Unpack\\~~Viggen_AddonStandalone_P\\Localization\\Game\\Cmn.dat";
            string datPath = "E:\\MODDING\\_Ace Combat 7\\_tools\\UnrealPak_Enhanced\\Unpack\\~~Viggen_AddonStandalone_P\\Localization\\Game\\A.dat";

            CmnFile Cmn = new CmnFile(cmnPath);
            DatFile dat = new DatFile(datPath);

            int test = Cmn["AircraftShort_Name_a10a"];

            Cmn.AddVariable("AircraftShort_Name_a10b", Cmn.Root);
            Cmn.AddVariable("AircraftShort_Name_a10bc", Cmn.Root);

            //int test = Cmn["AircraftShort_Name_a10a"];

            Console.WriteLine();
        }
    }
}
