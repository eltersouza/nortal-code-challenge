using System.ComponentModel.DataAnnotations;

namespace GameOfLife.Api.DTOs;

/// <summary>
/// Payload used to create a new board from a jagged array of boolean values.
/// </summary>
public class CreateBoardRequest : IValidatableObject
{
    [Required(ErrorMessage = "Cells array is required")]
    [MinLength(1, ErrorMessage = "Board must have at least 1 row")]
    public bool[][] Cells { get; set; } = Array.Empty<bool[]>();

    /// <summary>
    /// Validates that the uploaded board contains at least one column and forms a rectangular grid.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Cells.Length == 0)
        {
            yield break;
        }

        if (Cells.Any(row => row == null))
        {
            yield return new ValidationResult(
                "Cells cannot contain null rows.",
                [nameof(Cells)]);
            yield break;
        }

        if (Cells[0].Length == 0)
        {
            yield return new ValidationResult(
                "Board must have at least 1 column.",
                [nameof(Cells)]);
        }

        int expectedRowLength = Cells[0].Length;
        if (Cells.Any(row => row.Length != expectedRowLength))
        {
            yield return new ValidationResult(
                "All rows must have the same length.",
                [nameof(Cells)]);
        }
    }
}
