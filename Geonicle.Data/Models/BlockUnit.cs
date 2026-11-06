#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models;

public partial class BlockUnit
{
    /// <summary>Значения по умолчанию новой строки (как у столбцов БД): флажки AdR и ThR включены, название пустое.</summary>
    public BlockUnit()
    {
        AdR = true;
        ThR = true;
        HoleName = "";
    }

    public int BlockId { get; set; }

    public int VersionId { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime DateModified { get; set; }

    public int? HoleSeamId { get; set; }

    public string HoleName { get; set; }

    public decimal? AdCoal { get; set; }

    public decimal? AdFull { get; set; }

    public decimal? ThCoal { get; set; }

    public decimal? ThFull { get; set; }

    public bool? AdR { get; set; }

    public bool? ThR { get; set; }

    public int Id { get; set; }

    public virtual Block Block { get; set; }

    /// <summary>Глубина «От» пластопересечения скважины блока (SeamDefs по HoleSeamId); null — скважина не из пластопересечения. Только чтение.</summary>
    [NotMapped]
    public decimal? DepthFrom { get; private set; }

    /// <summary>Глубина «До» пластопересечения скважины блока. Только чтение.</summary>
    [NotMapped]
    public decimal? DepthTo { get; private set; }

    /// <summary>Задаёт глубины пластопересечения скважины блока (заполняет сервис экрана).</summary>
    public void SetSeamInfo(decimal? depthFrom, decimal? depthTo)
    {
        DepthFrom = depthFrom;
        DepthTo = depthTo;
    }
}