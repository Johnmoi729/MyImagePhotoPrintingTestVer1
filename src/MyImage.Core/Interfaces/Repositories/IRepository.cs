using MyImage.Core.Entities;

namespace MyImage.Core.Interfaces.Repositories;

/// <summary>
/// Generic repository interface for common CRUD operations.
/// Provides a base contract that can be implemented by specific repositories.
/// Reduces code duplication and provides consistent patterns across all repositories.
/// </summary>
/// <typeparam name="T">Entity type that this repository manages</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// Generic method that works with any entity type having an Id property.
    /// </summary>
    /// <param name="id">Entity's unique identifier</param>
    /// <returns>Entity or null if not found</returns>
    Task<T?> GetByIdAsync(string id);

    /// <summary>
    /// Retrieves all entities of this type.
    /// Should be used carefully with large collections - consider pagination.
    /// </summary>
    /// <returns>Collection of all entities</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Creates a new entity in the database.
    /// Should handle ID generation and timestamp setting automatically.
    /// </summary>
    /// <param name="entity">Entity to create</param>
    /// <returns>Created entity with generated ID</returns>
    Task<T> CreateAsync(T entity);

    /// <summary>
    /// Updates an existing entity.
    /// Should handle timestamp updates and optimistic concurrency if needed.
    /// </summary>
    /// <param name="id">Entity's unique identifier</param>
    /// <param name="entity">Updated entity data</param>
    /// <returns>True if update succeeded</returns>
    Task<bool> UpdateAsync(string id, T entity);

    /// <summary>
    /// Deletes an entity from the database.
    /// Implementation may be soft or hard delete depending on business requirements.
    /// </summary>
    /// <param name="id">Entity's unique identifier</param>
    /// <returns>True if deletion succeeded</returns>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Checks if an entity exists with the given ID.
    /// More efficient than GetByIdAsync when only existence check is needed.
    /// </summary>
    /// <param name="id">Entity's unique identifier</param>
    /// <returns>True if entity exists</returns>
    Task<bool> ExistsAsync(string id);
}