#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Data.Models;

public partial class Block
{
    public int BlockId { get; set; }

    public string BlockName { get; set; }

    public int Category { get; set; }

    public decimal? AdCoal { get; set; }

    public decimal? AdFull { get; set; }

    public decimal? ThCoal { get; set; }

    public decimal? ThFull { get; set; }

    public decimal? Dip { get; set; }

    public int PartId { get; set; }

    public bool Balance { get; set; }

    public string Comments { get; set; }

    public bool AvgCalc { get; set; }

    public bool Lock { get; set; }

    public int ConditionId { get; set; }

    public int VersionId { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime DateModified { get; set; }

    public int CoalType { get; set; }

    public bool GeomChanged { get; set; }

    public decimal? AdC { get; set; }

    public decimal? AdF { get; set; }

    public decimal? ThC { get; set; }

    public decimal? ThF { get; set; }

    public int Feature { get; set; }

    public virtual ICollection<BlockDetail> BlockDetails { get; set; } = new ObservableCollection<BlockDetail>();

    public virtual ICollection<BlockUnit> BlockUnits { get; set; } = new ObservableCollection<BlockUnit>();

    public virtual Condition Condition { get; set; }

    public virtual Part Part { get; set; }

    public virtual Version Version { get; set; }
}