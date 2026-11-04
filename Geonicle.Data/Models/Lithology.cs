#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models;

public partial class Lithology
{
    public int LithologyId { get; set; }

    public int BoreholeId { get; set; }

    public decimal DepthFrom { get; set; }

    public decimal DepthTo { get; set; }

    public decimal Angle { get; set; }

    public decimal Th { get; set; }

    public decimal ThNorm { get; set; }

    public decimal? Core { get; set; }

    public decimal? CoreNorm { get; set; }

    public decimal? CoreProc { get; set; }

    public int? RockId { get; set; }

    public string Description { get; set; }

    public bool SolidRock { get; set; }

    public int? Formation { get; set; }

    public double? X { get; set; }

    public double? Y { get; set; }

    public double? Z { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime DateModified { get; set; }

    public virtual Borehole Borehole { get; set; }

    public virtual ICollection<LithRe> LithRes { get; set; } = new ObservableCollection<LithRe>();

    public virtual ICollection<SeamDef> SeamDefDepthFromNavigations { get; set; } = new ObservableCollection<SeamDef>();

    public virtual ICollection<SeamDef> SeamDefDepthToNavigations { get; set; } = new ObservableCollection<SeamDef>();

    /// <summary>
    /// Название пласта, в пластопересечение которого входит слой (колонка «Пласт» в гриде литологии).
    /// Только для показа: заполняет HoleDataService при загрузке и после сохранения, в БД не пишется.
    /// </summary>
    [NotMapped]
    public string SeamName { get; set; }
}