using System;
using System.Collections.Generic;

namespace Demo09.Models;

public partial class Client
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
