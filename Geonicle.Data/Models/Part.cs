#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Data.Models;

public partial class Part
{
    public int PartId { get; set; }

    public int N { get; set; }

    public string Npart { get; set; }

    public bool Vertical { get; set; }

    public int SeamId { get; set; }

    public string Description { get; set; }

    public string Chapter { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime DateModified { get; set; }

    public virtual ICollection<Block> Blocks { get; set; } = new ObservableCollection<Block>();

    public virtual Seam Seam { get; set; }
}