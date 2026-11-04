#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Data.Models;

public partial class Seam
{
    /// <summary>
    /// Код пласта
    /// </summary>
    public int SeamId { get; set; }

    /// <summary>
    /// Название пласта
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Короткое название
    /// </summary>
    public string ShortName { get; set; }

    /// <summary>
    /// Стратиграфия
    /// </summary>
    public double? Strata { get; set; }

    public double? N { get; set; }

    public bool Actual { get; set; }

    public string GroupSeamId { get; set; }

    public decimal AdRoof { get; set; }

    public decimal AdBed { get; set; }

    public decimal DdaRoof { get; set; }

    public decimal DdaBed { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime DateModified { get; set; }

    public virtual ICollection<Average> Averages { get; set; } = new ObservableCollection<Average>();

    public virtual ICollection<Coeff> Coeffs { get; set; } = new ObservableCollection<Coeff>();

    public virtual ICollection<Part> Parts { get; set; } = new ObservableCollection<Part>();

    public virtual ICollection<PlanGeom> PlanGeoms { get; set; } = new ObservableCollection<PlanGeom>();

    public virtual ICollection<SeamCond> SeamConds { get; set; } = new ObservableCollection<SeamCond>();

    public virtual ICollection<SeamDef> SeamDefs { get; set; } = new ObservableCollection<SeamDef>();
}