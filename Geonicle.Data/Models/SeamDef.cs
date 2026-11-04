#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models;

public partial class SeamDef
{
    public int HoleSeamId { get; set; }

    public int BoreholeId { get; set; }

    public int SeamId { get; set; }

    public decimal? Dip { get; set; }

    public bool Detail { get; set; }

    public string Kb { get; set; }

    public bool OnMap { get; set; }

    public int CoalGrade { get; set; }

    public int CoalType { get; set; }

    public decimal DrawFrom { get; set; }

    public decimal DrawTo { get; set; }

    public int? InterbedId { get; set; }

    public string Comments { get; set; }

    public bool Stat { get; set; }

    public decimal? ResFrom { get; set; }

    public decimal? ResTo { get; set; }

    public int DepthFromId { get; set; }

    public int DepthToId { get; set; }

    public int? ResFromId { get; set; }

    public int? ResToId { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime DateModified { get; set; }

    public decimal? AdCoal { get; set; }

    public decimal? AdFull { get; set; }

    public decimal? ThCoal { get; set; }

    public decimal? ThFull { get; set; }

    public bool AdR { get; set; }

    public bool ThR { get; set; }

    public bool AvgCalc { get; set; }

    public bool Lock { get; set; }

    public virtual Borehole Borehole { get; set; }

    public virtual ICollection<Composite> Composites { get; set; } = new ObservableCollection<Composite>();

    public virtual Lithology DepthFromNavigation { get; set; }

    public virtual Lithology DepthToNavigation { get; set; }

    public virtual ICollection<Hssamp> Hssamps { get; set; } = new ObservableCollection<Hssamp>();

    public virtual Seam Seam { get; set; }

    /// <summary>Глубина «От»: кровля слоя литологии DepthFromId (как в представлении HoleSeams). Только чтение.</summary>
    [NotMapped]
    public decimal? DepthFrom => DepthFromNavigation?.DepthFrom;

    /// <summary>Глубина «До»: подошва слоя литологии DepthToId (как в представлении HoleSeams). Только чтение.</summary>
    [NotMapped]
    public decimal? DepthTo => DepthToNavigation?.DepthTo;
}