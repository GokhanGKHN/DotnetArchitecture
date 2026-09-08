namespace DotnetArchitecture.Domain.Enums;

public enum OrderStatus
{
    Pending = 1,    // Beklemede                                                                                                                       
    Completed = 2,  // Tamamlandı / Ödendi                                                                                                             
    Cancelled = 3   // İptal Edildi                                                                                                                    
}