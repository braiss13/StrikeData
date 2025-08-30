using Microsoft.EntityFrameworkCore;
using Npgsql; // Needed to detect Postgres unique-key violations (SqlState 23505)
using StrikeData.Models;

namespace StrikeData.Data.Extensions
{
    /// <summary>
    /// EF Core extensions to ensure and retrieve <see cref="StatCategory"/> records.
    /// Centralizes the "get or create" logic and handles basic concurrency.
    /// </summary>
    public static class StatCategoryExtensions
    {
        /// <summary>
        /// Ensures a <see cref="StatCategory"/> with the given <paramref name="name"/> exists
        /// and returns its database Id. If it does not exist, it is created and saved
        /// immediately so the Id is materialized.
        /// </summary>
        /// <remarks>
        /// - Checks the DbContext local cache first (avoids a roundtrip if already tracked).
        /// - Handles a potential race (another process inserts the same name) by catching
        ///   Postgres unique violations and reloading the existing row.
        /// </remarks>
        public static async Task<int> EnsureCategoryIdAsync(
            this AppDbContext context,
            string name,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name must be provided.", nameof(name));

            // 1) Check the local change tracker (already tracked in this context?)
            var local = context.StatCategories.Local.FirstOrDefault(c => c.Name == name);
            if (local != null) return local.Id;

            // 2) Check the database
            var existing = await context.StatCategories.FirstOrDefaultAsync(c => c.Name == name, ct);
            if (existing != null) return existing.Id;

            // 3) Create and save to get a real Id
            var created = new StatCategory { Name = name };
            context.StatCategories.Add(created);

            try
            {
                await context.SaveChangesAsync(ct);
                return created.Id;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Another transaction inserted the same category name.
                // Reload the existing row and return its Id.
                var again = await context.StatCategories.FirstAsync(c => c.Name == name, ct);
                return again.Id;
            }
        }

        /// <summary>
        /// Returns true if the exception indicates a Postgres unique-constraint violation.
        /// </summary>
        private static bool IsUniqueViolation(DbUpdateException ex)
            => ex.InnerException is PostgresException pg && pg.SqlState == "23505";
    }
}
