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
    /// Только для показа: заполняет BoreholeDataService при загрузке и после сохранения, в БД не пишется.
    /// </summary>
    [NotMapped]
    public string SeamName { get; set; }

    private LithRe reservesLink;

    /// <summary>
    /// «В подсчёт» слоя для выбранного прослоя (LithRes.Reserves, строку создаёт триггер БД при добавлении слоя).
    /// null — строки LithRes нет (дефект данных: значение не показывается и не правится). Только для показа и правки в гриде.
    /// </summary>
    [NotMapped]
    public bool? Reserves
    {
        get => reservesLink?.Reserves;
        set
        {
            if (reservesLink != null && value != null)
                reservesLink.Reserves = value.Value;
        }
    }

    /// <summary>Привязывает строку LithRes выбранного прослоя (задаёт сервис экрана).</summary>
    public void SetReservesLink(LithRe link) => reservesLink = link;
}