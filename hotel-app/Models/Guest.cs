using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace hotel_app.Models;

public class Guest
{
    public int Id { get; set; }

    [Required] public string FirstName { get; set; }
    [Required] public string LastName { get; set; }

    [Display(Name = "Е-адреса")] public string Email { get; set; }

    [RegularExpression("^\\d{9}$", ErrorMessage = "Телефонскиот број мора да биде составен од точно 9 цифри.")]
    public string PhoneNumber { get; set; }

    public DateTime RegistrationDate { get; set; }

    [ValidateNever] public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}