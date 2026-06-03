using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace hotel_app.Models;

public class Hotel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public double Rating { get; set; }

    [ValidateNever] public ICollection<Room> Rooms { get; set; } = new List<Room>();
}