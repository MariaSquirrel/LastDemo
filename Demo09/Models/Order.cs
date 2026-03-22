using System;
using System.Collections.Generic;

namespace Demo09.Models;

public partial class Order
{
    public int Id { get; set; }

    public string Numberorder { get; set; } = null!;

    public DateTime? Datacreate { get; set; }

    public DateTime? Datedeliver { get; set; }

    public int? PvzId { get; set; }

    public int? ClientId { get; set; }

    public string? Codegive { get; set; }

    public int? StatusId { get; set; }

    public int? UserId { get; set; }

    public virtual Client? Client { get; set; }

    public virtual ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();

    public virtual Pvz? Pvz { get; set; }

    public virtual Status? Status { get; set; }

    public virtual User? User { get; set; }
}
