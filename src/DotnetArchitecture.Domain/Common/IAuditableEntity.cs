namespace DotnetArchitecture.Domain.Common;

/// <summary>
/// Oluşturulma ve güncellenme audit (denetim izi) bilgilerini tutan varlık arayüzü.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    string? CreatedBy { get; set; }
    DateTime? LastModifiedAtUtc { get; set; }
    string? LastModifiedBy { get; set; }
}
