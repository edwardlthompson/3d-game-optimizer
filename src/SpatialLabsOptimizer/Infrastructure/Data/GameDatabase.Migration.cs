namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed partial class GameDatabase
{
    private async Task MigrateSchemaAsync(CancellationToken cancellationToken)
    {
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            ALTER TABLE games ADD COLUMN is_catalog_title INTEGER NOT NULL DEFAULT 0;
            """;
        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase))
        {
        }

        await using var views = _connection.CreateCommand();
        views.CommandText = """
            DROP VIEW IF EXISTS v_ready_to_play;
            DROP VIEW IF EXISTS v_one_click_ready;
            DROP VIEW IF EXISTS v_multiplayer_active;
            DROP VIEW IF EXISTS v_compatible_3d;
            CREATE VIEW v_ready_to_play AS
                SELECT * FROM games
                WHERE is_installed = 1 AND launch_readiness IN (0, 1) AND tier <= 4;
            CREATE VIEW v_one_click_ready AS
                SELECT * FROM games
                WHERE is_installed = 1 AND launch_readiness = 0 AND tier <= 4;
            CREATE VIEW v_multiplayer_active AS
                SELECT * FROM games
                WHERE current_players >= 500;
            CREATE VIEW v_compatible_3d AS
                SELECT * FROM games
                WHERE is_catalog_title = 1
                  AND is_installed = 1
                  AND tier < 5
                  AND launch_readiness IN (0, 1);
            """;
        await views.ExecuteNonQueryAsync(cancellationToken);
    }
}
