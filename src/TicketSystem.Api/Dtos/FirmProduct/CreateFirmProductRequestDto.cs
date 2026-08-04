using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.FirmProduct;

 public sealed class CreateFirmProductRequestDto
 {
      [Required]
      public int FirmId { get; set; }
      [Required]
      public int ProductId { get; set; }
 }
