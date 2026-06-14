using Microsoft.Data.Sqlite;

using SpatialLabsOptimizer.Infrastructure.Settings;



namespace SpatialLabsOptimizer.Infrastructure;



/// <summary>Milestone feature gates — v1.0.1+ on by default; v2 requires explicit opt-in.</summary>

public static class FeatureFlags

{

    public const string PostV1EnvVar = "SPATIALLABS_ENABLE_POST_V1";

    public const string V2EnvVar = "SPATIALLABS_ENABLE_V2";



    public static bool V101Enabled => true;



    public static bool V11Enabled =>

        V101Enabled && (

            string.Equals(Environment.GetEnvironmentVariable(PostV1EnvVar), "true", StringComparison.OrdinalIgnoreCase) ||

            !string.Equals(Environment.GetEnvironmentVariable(PostV1EnvVar), "false", StringComparison.OrdinalIgnoreCase));



    public static bool V2EnabledFromEnvironment =>

        string.Equals(Environment.GetEnvironmentVariable(V2EnvVar), "true", StringComparison.OrdinalIgnoreCase);



    public static bool V2Enabled =>

        V2EnabledFromEnvironment || ReadV2SettingsToggleFromStore();



    /// <summary>Whether v2 DI services were registered at host build (requires restart when prefs differ).</summary>

    public static bool V2RegisteredAtStartup { get; set; }



    public static async Task<bool> IsV2EnabledAsync(UserPreferencesService prefs, CancellationToken cancellationToken = default)

    {

        if (V2EnabledFromEnvironment)

        {

            return true;

        }



        return await prefs.GetV2ExperimentalAsync(cancellationToken);

    }



    private static bool ReadV2SettingsToggleFromStore()

    {

        try

        {

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var dbPath = Path.Combine(appData, "3d-game-optimizer", "settings.db");

            if (!File.Exists(dbPath))

            {

                return false;

            }



            using var connection = new SqliteConnection($"Data Source={dbPath}");

            connection.Open();

            using var cmd = connection.CreateCommand();

            cmd.CommandText = "SELECT value FROM settings WHERE key = $key";

            cmd.Parameters.AddWithValue("$key", UserPreferencesService.V2ExperimentalKey);

            var result = cmd.ExecuteScalar() as string;

            return result == "true";

        }

        catch (Exception)

        {

            return false;

        }

    }

}

