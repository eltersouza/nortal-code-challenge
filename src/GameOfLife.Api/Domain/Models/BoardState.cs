using System.Security.Cryptography;

namespace GameOfLife.Api.Domain.Models;

/// <summary>
/// Represents a board snapshot for a specific generation together with a deterministic hash.
/// </summary>
public class BoardState : IEquatable<BoardState>
{
    public bool[,] Cells { get; init; }
    public int Generation { get; init; }
    public string Hash { get; init; }

    /// <summary>
    /// Creates a board snapshot and computes a hash that can be used to detect repeated states.
    /// </summary>
    public BoardState(bool[,] cells, int generation)
    {
        Cells = cells ?? throw new ArgumentNullException(nameof(cells));
        Generation = generation;
        Hash = ComputeHash(cells);
    }

    /// <summary>
    /// Generates a stable SHA-256 hash from the board contents.
    /// </summary>
    private static string ComputeHash(bool[,] cells)
    {
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);
        var bytes = new byte[width * height];

        int index = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bytes[index++] = cells[x, y] ? (byte)1 : (byte)0;
            }
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Compares two board states using their hash values.
    /// </summary>
    public bool Equals(BoardState? other)
    {
        if (other is null) return false;
        return Hash == other.Hash;
    }

    /// <summary>
    /// Compares this board state with another object instance.
    /// </summary>
    public override bool Equals(object? obj) => Equals(obj as BoardState);

    /// <summary>
    /// Returns a hash code derived from the computed board hash.
    /// </summary>
    public override int GetHashCode() => Hash.GetHashCode();
}
