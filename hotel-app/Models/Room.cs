using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace hotel_app.Models;

public class Room
{
    public int Id { get; set; }

    public string RoomNumber { get; set; }

    public string Type { get; set; }

    public string Description { get; set; }

    public string ImageUrl { get; set; }

    public int Capacity { get; set; }

    public int HotelId { get; set; }

    [ValidateNever] public Hotel Hotel { get; set; }

    [ValidateNever] public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}