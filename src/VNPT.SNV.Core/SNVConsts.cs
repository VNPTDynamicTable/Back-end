using VNPT.SNV.Debugging;

namespace VNPT.SNV;

public class SNVConsts
{
    public const string LocalizationSourceName = "SNV";

    public const string ConnectionStringName = "Default";

    public const bool MultiTenancyEnabled = true;


    /// <summary>
    /// Default pass phrase for SimpleStringCipher decrypt/encrypt operations
    /// </summary>
    public static readonly string DefaultPassPhrase =
        DebugHelper.IsDebug ? "gsKxGZ012HLL3MI5" : "3ac5faa37fb24411b3f2e2a6bb4a8aa3";
}
