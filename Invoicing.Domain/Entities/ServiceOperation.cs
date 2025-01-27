using System.ComponentModel.DataAnnotations;
using Invoicing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.Domain.Entities;

[Index(nameof(ServiceProvisionId), nameof(Date), IsDescending = [false, true])]
public sealed class ServiceOperation : BaseEntity
{
    public DateOnly Date { get; set; }
    public ServiceOperationType Type { get; set; }

    [Required] public Guid ServiceProvisionId { get; set; }
    public ServiceProvision ServiceProvision { get; set; } = null!;
}