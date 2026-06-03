using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace hotel_app.Models;

public class Reservation
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    [ValidateNever] public Room Room { get; set; }

    public int GuestId { get; set; }
    [ValidateNever] public Guest Guest { get; set; }

    public DateTime CheckInDate { get; set; }

    // nullable: null = the guest has not checked out yet
    public DateTime? CheckOutDate { get; set; }
}