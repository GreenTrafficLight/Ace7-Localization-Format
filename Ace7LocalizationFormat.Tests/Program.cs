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

            Cmn.AddVariable("AircraftShort_Name_a10a", Cmn.Root);
            Cmn.AddVariable("AircraftShort_Name_trndf3", Cmn.Root);
            Cmn.AddVariable("AircraftShort_Name_trnd", Cmn.Root);

            int test = Cmn["AircraftShort_Name_trnd"];

            Console.WriteLine();
        }
    }
}
