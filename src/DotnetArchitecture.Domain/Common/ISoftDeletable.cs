namespace DotnetArchitecture.Domain.Common;

/// <summary>
/// Fiziksel olarak silinmek yerine mantıksal (soft delete) olarak silinebilen varlık arayüzü.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
    string? DeletedBy { get; set; }
}
