namespace TicketSystem.Dtos.FirmProduct;

public enum FirmProductCreateStatus
{
    Created,

    /// <summary>The firm already has this product assigned.</summary>
    DuplicateAssignment,

    /// <summary>The referenced firm or product does not exist.</summary>
    UnknownFirmOrProduct
}

/// <summary>
/// Lets the repository report why an assignment could not be created without using exceptions for control flow.
/// </summary>
public readonly record struct FirmProductCreateResult(
    FirmProductCreateStatus Status,
    FirmProductDto? Assignment)
{
    public static FirmProductCreateResult Created(FirmProductDto assignment) =>
        new(FirmProductCreateStatus.Created, assignment);

    public static FirmProductCreateResult Duplicate() =>
        new(FirmProductCreateStatus.DuplicateAssignment, null);

    public static FirmProductCreateResult UnknownFirmOrProduct() =>
        new(FirmProductCreateStatus.UnknownFirmOrProduct, null);
}
