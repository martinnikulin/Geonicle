#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models;

public partial class Average
{
    public int InterbedId { get; set; }

    public int SeamId { get; set; }

    public int CoalType { get; set; }

    public int CoalGrade { get; set; }

    public int VarId { get; set; }

    public double? Value { get; set; }

    public double? UserValue { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime DateModified { get; set; }

    public virtual Seam Seam { get; set; }

    /// <summary>
    /// Строка ещё не сохранена (DateCreated выставляет БД при вставке). Показатель, тип и марка — части ключа: у сохранённой
    /// строки их менять нельзя, у новой можно.
    /// </summary>
    [NotMapped]
    public bool IsNew => DateCreated == default;
}