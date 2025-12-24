using System;
using System.ComponentModel.DataAnnotations;

namespace talim_platforma.Models;

public class Advertisement
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    // "all", "home", "teacher", "student"
    [Required, MaxLength(50)]
    public string Target { get; set; } = "all";

    [MaxLength(300)]
    public string? ImagePath { get; set; }

    [MaxLength(400)]
    public string? LinkUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

