using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ConferenceRoomBooking.Api.Dtos;

public class AvailableRoomsQuery
{
    /// <summary>Date, e.g. 2026-10-01</summary>
    [BindRequired]
    public DateOnly Date { get; set; }

    /// <summary>Start time, e.g. 10:00</summary>
    [BindRequired]
    public TimeOnly StartTime { get; set; }

    /// <summary>End time, e.g. 14:00</summary>
    [BindRequired]
    public TimeOnly EndTime { get; set; }

    [Range(1, 1000)]
    public int Capacity { get; set; }
}