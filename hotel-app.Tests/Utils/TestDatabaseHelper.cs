using System.Linq.Expressions;
using hotel_app.Data;
using hotel_app.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelApp.Tests.Utils;

public static class TestDatabaseHelper
{
    public static void SeedDatabase(ApplicationDbContext context)
    {
        var hotel1 = new Hotel
        {
            Name = "Гранд Хотел",
            Address = "ул. Македонија 1",
            City = "Скопје",
            Country = "Македонија",
            Rating = 8.5
        };
        var hotel2 = new Hotel
        {
            Name = "Хотел Метропол",
            Address = "ул. Кеј 5",
            City = "Охрид",
            Country = "Македонија",
            Rating = 9.0
        };
        var hotel3 = new Hotel
        {
            Name = "Универзитетски Хотел",
            Address = "бул. Партизански 16",
            City = "Скопје",
            Country = "Македонија",
            Rating = 7.8
        };

        context.Hotels.AddRange(hotel1, hotel2, hotel3);
        context.SaveChanges();

        var room1 = new Room
        {
            RoomNumber = "101",
            Type = "Single",
            Description = "Соба со еден кревет",
            ImageUrl = "https://example.com/rooms/101.jpg",
            Capacity = 1,
            HotelId = hotel1.Id
        };
        var room2 = new Room
        {
            RoomNumber = "102",
            Type = "Double",
            Description = "Соба со два кревети",
            ImageUrl = "https://example.com/rooms/102.jpg",
            Capacity = 2,
            HotelId = hotel1.Id
        };
        var room3 = new Room
        {
            RoomNumber = "201",
            Type = "Suite",
            Description = "Апартман",
            ImageUrl = "https://example.com/rooms/201.jpg",
            Capacity = 4,
            HotelId = hotel2.Id
        };

        context.Rooms.AddRange(room1, room2, room3);
        context.SaveChanges();

        var guest1 = new Guest
        {
            FirstName = "Александар",
            LastName = "Петровски",
            Email = "aleksandar@example.com",
            PhoneNumber = "070123456",
            RegistrationDate = new DateTime(2023, 1, 15)
        };
        var guest2 = new Guest
        {
            FirstName = "Марија",
            LastName = "Стојановска",
            Email = "marija@example.com",
            PhoneNumber = "071234567",
            RegistrationDate = new DateTime(2023, 3, 20)
        };
        var guest3 = new Guest
        {
            FirstName = "Никола",
            LastName = "Димитровски",
            Email = "nikola@example.com",
            PhoneNumber = "072345678",
            RegistrationDate = new DateTime(2024, 5, 10)
        };

        context.Guests.AddRange(guest1, guest2, guest3);
        context.SaveChanges();

        // Reservation that is still active (CheckOutDate == null)
        var reservation1 = new Reservation
        {
            RoomId = room1.Id,
            GuestId = guest1.Id,
            CheckInDate = DateTime.Now.AddDays(-10),
            CheckOutDate = null
        };
        // Reservation that has been checked out
        var reservation2 = new Reservation
        {
            RoomId = room2.Id,
            GuestId = guest1.Id,
            CheckInDate = DateTime.Now.AddDays(-30),
            CheckOutDate = DateTime.Now.AddDays(-5)
        };
        // Another active reservation for room1
        var reservation3 = new Reservation
        {
            RoomId = room1.Id,
            GuestId = guest2.Id,
            CheckInDate = DateTime.Now.AddDays(-3),
            CheckOutDate = null
        };

        context.Reservations.AddRange(reservation1, reservation2, reservation3);
        context.SaveChanges();
    }

    public static async Task ResetDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        SeedDatabase(context);
    }

    public static async Task<int> GetCount<T>(IServiceProvider serviceProvider) where T : class
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<T>().CountAsync();
    }

    public static async Task<T> GetFirst<T>(IServiceProvider serviceProvider) where T : class
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<T>().FirstAsync();
    }

    public static async Task<List<T>> GetAllWhere<T>(
        IServiceProvider services,
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null)
        where T : class
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        IQueryable<T> query = dbContext.Set<T>().Where(predicate);
        if (include != null)
            query = include(query);

        return await query.ToListAsync();
    }

    public static T? GetById<T>(IServiceProvider serviceProvider, Func<T, bool> predicate) where T : class
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return context.Set<T>().Where(predicate).FirstOrDefault();
    }

    public static async Task<T> CreateEntity<T>(IServiceProvider services, T entity) where T : class
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Set<T>().AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }
}