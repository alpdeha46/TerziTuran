using System.ComponentModel.DataAnnotations;

namespace TerziTuran.Web.Models;

public enum UserRole
{
    [Display(Name = "Yönetici")]
    Admin = 1,
    [Display(Name = "Personel")]
    Staff = 2,
    [Display(Name = "Müşteri")]
    Customer = 3
}

public enum OrderStatus
{
    [Display(Name = "Beklemede")]
    Pending = 1,
    [Display(Name = "Ölçü Alındı")]
    Measured = 2,
    [Display(Name = "Dikimde")]
    Sewing = 3,
    [Display(Name = "Provada")]
    Fitting = 4,
    [Display(Name = "Hazır")]
    Ready = 5,
    [Display(Name = "Teslim Edildi")]
    Delivered = 6,
    [Display(Name = "İptal")]
    Cancelled = 7
}

public enum OrderPriority
{
    [Display(Name = "Düşük")]
    Low = 1,
    [Display(Name = "Orta")]
    Medium = 2,
    [Display(Name = "Yüksek")]
    High = 3,
    [Display(Name = "Acil")]
    Urgent = 4
}

public enum PaymentType
{
    [Display(Name = "Nakit")]
    Cash = 1,
    [Display(Name = "Kart")]
    Card = 2,
    [Display(Name = "Havale / EFT")]
    Transfer = 3
}

public enum AppointmentStatus
{
    [Display(Name = "Planlandı")]
    Scheduled = 1,
    [Display(Name = "Tamamlandı")]
    Completed = 2,
    [Display(Name = "İptal Edildi")]
    Cancelled = 3
}

public enum OrderServiceType
{
    [Display(Name = "Dikim")]
    Sewing = 1,
    [Display(Name = "Tamir")]
    Repair = 2
}
